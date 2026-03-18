// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using Polytoria.Datamodel;
using Polytoria.Networking.Synchronizers;
using Polytoria.Schemas.Debugger;
using Polytoria.Scripting;
using Polytoria.Shared;
using Polytoria.Utils;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;

namespace Polytoria.Client;

public static class DebugClient
{
	public static bool ClientStarted { get; private set; } = false;
	private static TcpClient _client = null!;
	private static NetworkStream _stream = null!;
	private static readonly List<KeyValuePair<string, TaskCompletionSource<MessageNewServerResponse>>> _pendingServerInstance = [];

	private static string _address = "";

	public static async Task Start(string addresss, int port, string? debugID = null)
	{
		if (ClientStarted) return;

		_address = addresss;
		_client = new TcpClient();

		await _client.ConnectAsync(addresss, port);

		_stream = _client.GetStream();

		// Start receiving messages in background
		_ = Task.Run(ReceiveMessages);

		if (debugID != null)
		{
			await SendMessage(new MessageClientData() { DebugID = debugID });
		}
		await SendMessage(new MessageReportProcess() { ProcessID = Globals.IsMobileBuild ? 0 : OS.GetProcessId() });

		ClientStarted = true;

		PT.PrintV($"-- Connected to debug server --");
	}

	private static async Task ReceiveMessages()
	{
		while (true)
		{
			if (!_client.Connected) { ClientStarted = false; break; }
			byte[] buffer = new byte[1024];

			try
			{
				int bytesRead = await _stream.ReadAsync(buffer);

				if (bytesRead == 0)
				{
					PT.PrintV("Debug server closed connection");
					break;
				}

				IDebugMessage? msg = SerializeUtils.Deserialize<IDebugMessage>(buffer);
				if (msg != null)
				{
					OnMessageRecv(msg);
				}
			}
			catch (Exception e)
			{
				PT.PrintErrV(e);
				PT.PrintErrV($"Receive error: {e.Message}");
			}
		}
	}

	private static void OnMessageRecv(IDebugMessage msg)
	{
		if (msg is MessageShutdown)
		{
			if (!Globals.IsMobileBuild)
			{
				Globals.Singleton.Quit();
			}
		}
		else if (Globals.IsMobileBuild)
		{
			if (msg is MessageLaunchWorld)
			{
				Node app = Globals.Singleton.SwitchEntry(Globals.AppEntryEnum.Client);
				if (app is ClientEntry ce)
				{
					ClientEntry.ClientEntryData entryData = new()
					{
						ConnectAddress = _address,
						TestIsServer = false,
						TestUserID = 1144
					};
					ce.Entry(entryData);
				}
			}
		}
		else if (msg is MessageNewServerResponse ns)
		{
			foreach (var pair in _pendingServerInstance.ToArray())
			{
				if (pair.Key == ns.WorldPath)
				{
					pair.Value.SetResult(ns);
					_pendingServerInstance.Remove(pair);
				}
			}
		}
		else if (msg is MessageObjPropChange pc)
		{
			if (World.Current == null) return;
			if (!World.Current.Network.IsServer) return;
			NetworkedObject? obj = World.Current.GetObjectFromID(pc.ObjectID);
			if (obj != null)
			{
				PropertyInfo? prop = obj.GetSyncProperty(pc.PropertyName);
				if (prop != null)
				{
					object? val = NetworkPropSync.DeserializePropValue(pc.PropertyValue, prop.PropertyType);

					// Call in main thread
					Callable.From(() =>
					{
						prop.SetValue(obj, val);
					}).CallDeferred();
				}
			}
		}
	}

	public static async Task SendMessage(IDebugMessage msg)
	{
		if (!ClientStarted) return;
		byte[] data = SerializeUtils.Serialize(msg);
		try
		{
			await _stream.WriteAsync(data);
		}
		catch (Exception ex)
		{
			PT.PrintErrV(ex.Message);
		}
	}

	public static async Task SendServerReady()
	{
		await SendMessage(new MessageServerReady());
	}

	public static async Task SendLogDispatch(LogDispatcher.LogData data)
	{
		await SendMessage(new MessageLogDispatch()
		{
			LogType = data.LogType,
			LogFrom = data.LogFrom,
			Content = data.Content
		});
	}

	public static async Task<MessageNewServerResponse> CreateServerInstance(string toPath)
	{
		TaskCompletionSource<MessageNewServerResponse> restsk = new();
		_pendingServerInstance.Add(new(toPath, restsk));
		await SendMessage(new MessageNewServerRequest() { WorldPath = toPath });
		return await restsk.Task;
	}
}
