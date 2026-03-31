// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using Polytoria.Networking.DataChannel;
using Polytoria.Networking.DataChannel.Schemas;
using Polytoria.Shared;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Polytoria.Networking;

/// <summary>
/// ENet network instance
/// </summary>
public class NetworkInstance
{
	private const float SilenceTimeoutSeconds = 5.0f;
	private const ENetConnection.CompressionMode CompressionMode = ENetConnection.CompressionMode.Zlib;
	private const int BandwidthInLimit = 0;
	private const int BandwidthOutLimit = 30 * 1024;
	private const int BandwidthPerPlayer = 200 * 1024; // 200 KB/s per player
	private long _lastMessageTicks = DateTime.UtcNow.Ticks;
	private readonly Dictionary<int, string> _dataServerTokens = [];

	private const int DefaultCapacity = 67;
	private const int DefaultPort = 21441;
	private const int MinimumTimeout = 0;

	private readonly ENetConnection _peer;
	public DataChannelServer? DataServer = null;
	public DataChannelClient? DataClient = null;

	private bool _dataChAuthd = false;

	private readonly ConcurrentQueue<Action> _actionQueue = new();
	internal readonly ConcurrentDictionary<int, ENetPacketPeer> IdToPeer = [];
	internal readonly ConcurrentDictionary<ENetPacketPeer, int> PeerToId = [];

	public ICollection<int> PeerIds => IdToPeer.Keys;

	private int _peerCounter = 1;
	private int _localPeerID = 0;

	public event Action<int>? PeerConnected;
	public event Action<int>? PeerDisconnected;
	public event Action? ClientConnected;
	public event Action? ClientDisconnected;
	public event Action? ClientError;
	public event MessageReceivedHandler? MessageReceived;

	public bool IsSilence { get; private set; } = false;
	public bool IsServer { get; private set; } = false;
	private bool _shutdownd = false;

	public NetworkInstance()
	{
		_peer = new();
	}

	public void CreateServer(int port = DefaultPort, int maxChannels = 3)
	{
		Error e = _peer.CreateHostBound("*", port, DefaultCapacity, maxChannels);
		_peer.Compress(CompressionMode);

		if (e != Error.Ok)
		{
			PT.PrintErr("Couldn't create host: ", e);
		}

		IsServer = true;

		DataServer = new();
		DataServer.Start(this, port);
		DataServer.MessageReceived += OnDataServerRecv;

		PostPeerCreate();
	}

	public async Task CreateClient(string address, int port, int maxChannels = 3)
	{
		Error e = _peer.CreateHost(DefaultCapacity, maxChannels);
		_peer.BandwidthLimit(BandwidthInLimit, BandwidthOutLimit);
		_peer.Compress(CompressionMode);

		if (e != Error.Ok)
		{
			PT.PrintErr("Couldn't create host: ", e);
			return;
		}

		_peer.ConnectToHost(address, port);

		DataClient = new();
		DataClient.MessageReceived += OnDataClientRecv;
		await DataClient.Start(address, port);

		PostPeerCreate();
	}

	private void OnDataServerRecv(int peerID, IDataServerMessage message)
	{
		if (message is MessageData data)
		{
			Callable.From(() => MessageReceived?.Invoke(peerID, data.Data, TransferMode.Reliable, true)).CallDeferred();
		}
	}

	private void OnDataClientRecv(IDataServerMessage message)
	{
		if (message is MessageData data)
		{
			Callable.From(() => MessageReceived?.Invoke(1, data.Data, TransferMode.Reliable, true)).CallDeferred();
		}
	}

	/// <summary>
	/// Adapt server bandwidth to player count
	/// </summary>
	/// <param name="playerCount"></param>
	public void AdaptBandwidth(int playerCount)
	{
		_peer.BandwidthLimit(0, playerCount * BandwidthPerPlayer);
	}

	private void PostPeerCreate()
	{
		_ = Task.Run(NetworkLoop);
	}

	internal bool VerifyDataServerToken(int peerID, string token)
	{
		if (_dataServerTokens.TryGetValue(peerID, out var val))
		{
			if (val == token)
			{
				// DataServer Token success, remove the token too
				_dataServerTokens.Remove(peerID);
				return true;
			}
		}
		return false;
	}

	public ENetPacketPeer? GetPacketPeerFromId(int id)
	{
		if (IdToPeer.TryGetValue(id, out ENetPacketPeer? p))
		{
			return p;
		}
		return null;
	}

	public void SendMessage(int targetID, byte[] data, TransferMode transferMode, int transferChannel = 0, bool useDataChannel = false)
	{
		if (useDataChannel)
		{
			DataChannelSendMessage(targetID, data);
			return;
		}
		_actionQueue.Enqueue(() =>
		{
			ENetPacketPeer? peer = GetPacketPeerFromId(targetID);
			if (peer == null)
			{
				GD.PushWarning(targetID, " doesn't exist");
				return;
			}
			Error err = peer.Send(transferChannel, data, (int)transferMode);
			if (err != Error.Ok)
			{
				GD.PushError("Send error: ", err);
			}
		});
	}

	public void DisconnectPeer(int targetID, bool force = false)
	{
		_actionQueue.Enqueue(() =>
		{
			ENetPacketPeer? peer = GetPacketPeerFromId(targetID);
			if (peer == null)
			{
				GD.PushWarning(targetID, " doesn't exist");
				return;
			}
			if (force)
			{
				peer.PeerDisconnectNow();
			}
			else
			{
				peer.PeerDisconnect();
			}
		});
	}

	public void Shutdown()
	{
		if (_shutdownd) return;
		_shutdownd = true;
		foreach ((_, ENetPacketPeer pk) in IdToPeer)
		{
			pk.PeerDisconnect();
		}
		_peer.Flush();
		_peer.Destroy();
	}

	public void BroadcastMessage(byte[] data, TransferMode transferMode, int transferChannel = 0, int[]? except = null, bool useDataChannel = false)
	{
		if (useDataChannel)
		{
			foreach (int id in IdToPeer.Keys)
			{
				DataChannelSendMessage(id, data);
			}
			return;
		}
		_actionQueue.Enqueue(() =>
		{
			foreach ((int id, ENetPacketPeer? peer) in IdToPeer)
			{
				if (!peer.IsActive()) continue;
				if (except != null && except.Contains(id)) continue;
				peer?.Send(transferChannel, data, (int)transferMode);
			}
		});
	}

	private void DataChannelSendMessage(int id, byte[] data)
	{
		if (IsServer)
		{
			_ = DataServer?.SendMessage(id, new MessageData() { Data = data });
		}
		else
		{
			_ = DataClient?.SendMessage(new MessageData() { Data = data });
		}
	}

	private void NetworkLoop()
	{
		while (true)
		{
			if (_shutdownd) return;
			if (!GodotObject.IsInstanceValid(_peer)) return;
			try
			{
				ProcessActionQueue();
				ProcessNetwork();
				CheckSilence();
				_peer.Flush();
				Thread.Sleep(1);
			}
			catch (Exception ex)
			{
				GD.PushError(ex);
			}
		}
	}

	public double PopStatistic(ENetConnection.HostStatistic hs)
	{
		return _peer.PopStatistic(hs);
	}

	private void ProcessNetwork()
	{
		while (true)
		{
			Godot.Collections.Array serviceData = _peer.Service(MinimumTimeout);

			ENetConnection.EventType eventType = (ENetConnection.EventType)(int)serviceData[0];
			ENetPacketPeer? fromPeer = (ENetPacketPeer?)serviceData[1];
			int peerID = 0;
			if (fromPeer != null)
			{
				if (PeerToId.TryGetValue(fromPeer, out int p))
				{
					peerID = p;
				}
			}

			if (eventType == ENetConnection.EventType.Connect)
			{
				if (fromPeer == null) { PT.PrintWarn("Connect received but peer is null, return"); return; }

				if (!IsServer)
				{
					peerID = 1;
				}
				else
				{
					_peerCounter++;
					peerID = _peerCounter;
				}

				IdToPeer[peerID] = fromPeer;
				PeerToId[fromPeer] = peerID;

				Callable.From(() =>
				{
					if (IsServer)
					{
						// Two Guid because im super paranoid
						string dataToken = Guid.NewGuid().ToString() + Guid.NewGuid().ToString();
						_dataServerTokens[peerID] = dataToken;

						PeerConnected?.Invoke(peerID);

						// Send handshake for data channel connect
						SendMessage(peerID, ("sauth:" + dataToken + ";" + peerID).ToUtf8Buffer(), TransferMode.Reliable);
					}
					else
					{
						ClientConnected?.Invoke();
					}
				}).CallDeferred();
			}
			else if (eventType == ENetConnection.EventType.Disconnect)
			{
				if (fromPeer == null) { PT.PrintWarn("Disconnect received but peer is null, return"); return; }
				IdToPeer.TryRemove(peerID, out _);
				PeerToId.TryRemove(fromPeer, out _);
				Callable.From(() =>
				{
					if (IsServer)
					{
						_dataServerTokens.Remove(peerID);
						PeerDisconnected?.Invoke(peerID);
					}
					else
					{
						ClientDisconnected?.Invoke();
					}
				}).CallDeferred();
			}
			else if (eventType == ENetConnection.EventType.Receive)
			{
				Interlocked.Exchange(ref _lastMessageTicks, DateTime.UtcNow.Ticks);
				if (fromPeer == null) { PT.PrintWarn("Message received but peer is null, return"); return; }
				while (fromPeer.GetAvailablePacketCount() > 0)
				{
					int pkf = fromPeer.GetPacketFlags();
					TransferMode m = pkf switch
					{
						(int)ENetPacketPeer.FlagReliable => TransferMode.Reliable,
						(int)ENetPacketPeer.FlagUnreliableFragment => TransferMode.UnreliableOrdered,
						(int)ENetPacketPeer.FlagUnsequenced => TransferMode.Unreliable,
						_ => TransferMode.Unreliable,
					};
					byte[] data = fromPeer.GetPacket();

					if (!IsServer && !_dataChAuthd)
					{
						string ps = data.GetStringFromUtf8();
						if (ps.StartsWith("sauth:"))
						{
							// Accept NetworkInstance handshake
							// NOTE: This is kinda hacky ngl... could have some rework here?
							string handshakeStr = ps.TrimPrefix("sauth:");
							var splited = handshakeStr.Split(';');
							string token = splited[0];
							string selfID = splited[1];
							_localPeerID = int.Parse(selfID);
							DataClient?.SendAuthenticate(token, _localPeerID).Wait();
							_dataChAuthd = true;
							continue;
						}
					}

					Callable.From(() => MessageReceived?.Invoke(peerID, data, m, false)).CallDeferred();
				}
			}
			else if (eventType == ENetConnection.EventType.Error)
			{
				PT.PrintErr("Client error");
				Callable.From(() =>
				{
					ClientError?.Invoke();
				}).CallDeferred();
			}
			else if (eventType == ENetConnection.EventType.None) return;
		}
	}

	private void CheckSilence()
	{
		// Only check silence in client
		if (IsServer) return;

		long lastTicks = Interlocked.Read(ref _lastMessageTicks);
		double elapsedSeconds = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - lastTicks).TotalSeconds;

		bool currentlySilent = elapsedSeconds > SilenceTimeoutSeconds;

		if (currentlySilent != IsSilence)
		{
			IsSilence = currentlySilent;
			if (IsSilence)
			{
				PT.PrintErr("[!] Network connection has gone silent");
			}
			else
			{
				PT.Print("[i] Network connection resumed.");
			}
		}
	}

	private void ProcessActionQueue()
	{
		while (_actionQueue.TryDequeue(out Action? action))
		{
			try
			{
				action?.Invoke();
			}
			catch (Exception ex)
			{
				GD.PushError("Error processing queued action: ", ex);
			}
		}
	}

	public bool IsPeerConnected(int peerID)
	{
		return IdToPeer.ContainsKey(peerID);
	}

	public delegate void MessageReceivedHandler(int peerID, byte[] data, TransferMode transferMode, bool fromDataChannel);
}

public enum AuthorityMode
{
	Server,
	Authority,
	Any
}


public enum TransferMode
{
	Reliable = (int)ENetPacketPeer.FlagReliable,
	UnreliableOrdered = (int)ENetPacketPeer.FlagUnreliableFragment,
	Unreliable = (int)ENetPacketPeer.FlagUnsequenced,
}

public class NetworkException(string err) : Exception(err) { }
