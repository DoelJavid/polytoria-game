using Polytoria.Networking.DataChannel.Schemas;
using Polytoria.Shared;
using Polytoria.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Polytoria.Networking.DataChannel;

/// <summary>
/// Data channel server, TCP protocol that lives alongside NetworkInstance's ENet UDP Protocol
/// Used to transfer large data, such as place replication. Large packet should be routed around this protocol to workaround the real-time packet size limit
/// </summary>
public class DataChannelServer
{
	private TcpListener _server = null!;
	private const int DefaultPort = 21441;
	private readonly List<TcpClient> _tcpClients = [];
	private readonly Dictionary<TcpClient, int> _clientToPeerID = [];
	private readonly Dictionary<int, TcpClient> _peerIDToClient = [];
	private NetworkInstance NetInstance = null!;
	public event Action<int, IDataServerMessage>? MessageReceived;

	public void Start(NetworkInstance netInstance, int port = DefaultPort)
	{
		NetInstance = netInstance;
		IPAddress localAddr = IPAddress.Parse("0.0.0.0");
		_server = new TcpListener(localAddr, port);
		_server.Start();
		_ = Task.Run(ServerMainLoop);
	}

	private async Task ServerMainLoop()
	{
		while (true)
		{
			TcpClient client = await _server.AcceptTcpClientAsync();

			_ = Task.Run(() => HandleClient(client));
		}
	}

	private async Task HandleClient(TcpClient client)
	{
		_tcpClients.Add(client);
		try
		{
			NetworkStream stream = client.GetStream();
			byte[] buffer = new byte[4096];

			while (true)
			{
				try
				{
					int bytesRead;
					try
					{
						bytesRead = await stream.ReadAsync(buffer);
					}
					catch (IOException ex) when (ex.InnerException is SocketException)
					{
						// Forceful disconnect
						break;
					}
					catch
					{
						// Socket errors ?
						break;
					}

					if (bytesRead == 0)
					{
						break; // Client disconnected gracefully
					}

					var msg = SerializeUtils.Deserialize<IDataServerMessage>(buffer);
					if (msg != null)
					{
						try
						{
							await OnMessageRecv(client, msg);
						}
						catch (Exception ex)
						{
							PT.PrintErr(ex);
						}
					}
				}
				catch (Exception ex)
				{
					PT.PrintErr(ex);
				}
			}
		}
		finally
		{
			// Client closes connection, deinit
			_clientToPeerID.Remove(client, out var peerID);
			_peerIDToClient.Remove(peerID);
			client.Close();
		}
	}

	private async Task OnMessageRecv(TcpClient from, IDataServerMessage msg)
	{
		if (msg is MessageAuthenticate authMsg)
		{
			if (NetInstance.VerifyDataServerToken(authMsg.PeerID, authMsg.AuthToken))
			{
				_clientToPeerID[from] = authMsg.PeerID;
				_peerIDToClient[authMsg.PeerID] = from;
				await SendMessage(from, new MessageAuthRes());
			}
		}
		else if (_clientToPeerID.TryGetValue(from, out int peerID))
		{
			MessageReceived?.Invoke(peerID, msg);
		}
	}

	public static async Task SendMessage(TcpClient client, IDataServerMessage msg)
	{
		byte[] payload = SerializeUtils.Serialize(msg);
		byte[] lengthPrefix = BitConverter.GetBytes(payload.Length);
		NetworkStream stream = client.GetStream();

		// Write length
		await stream.WriteAsync(lengthPrefix);

		// Then the data
		await stream.WriteAsync(payload);
	}

	public async Task SendMessage(int peerID, IDataServerMessage msg)
	{
		if (_peerIDToClient.TryGetValue(peerID, out var peer))
		{
			await SendMessage(peer, msg);
		}
	}
}
