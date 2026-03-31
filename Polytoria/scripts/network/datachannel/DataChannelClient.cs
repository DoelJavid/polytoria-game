using Polytoria.Networking.DataChannel.Schemas;
using Polytoria.Shared;
using Polytoria.Utils;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Polytoria.Networking.DataChannel;

public class DataChannelClient
{
	public bool ClientStarted { get; private set; } = false;
	private TcpClient _client = null!;
	private NetworkStream _stream = null!;
	public event Action<IDataServerMessage>? MessageReceived;
	private TaskCompletionSource? _authTcs;

	public async Task Start(string address, int port)
	{
		if (ClientStarted) return;

		_client = new TcpClient();

		await _client.ConnectAsync(address, port);

		_stream = _client.GetStream();

		// Start receiving messages in background
		_ = Task.Run(ReceiveMessages);

		ClientStarted = true;

		PT.PrintV($"[i] Connected to Data Server");
	}

	private async Task ReceiveMessages()
	{
		while (true)
		{
			if (!_client.Connected) { ClientStarted = false; break; }

			try
			{
				// Get message length
				byte[] lengthBuffer = new byte[4];
				if (!await ReadExactAsync(lengthBuffer, 4)) break;
				int messageLength = BitConverter.ToInt32(lengthBuffer);

				// Read the entire content
				byte[] payload = new byte[messageLength];
				if (!await ReadExactAsync(payload, messageLength)) break;

				// Deserialize complete payload
				var msg = SerializeUtils.Deserialize<IDataServerMessage>(payload);
				if (msg != null) OnMessageRecv(msg);
			}
			catch (Exception e)
			{
				PT.PrintErrV(e);
				PT.PrintErrV($"Receive error: {e.Message}");
			}
		}
	}

	private async Task<bool> ReadExactAsync(byte[] buffer, int count)
	{
		int totalRead = 0;
		while (totalRead < count)
		{
			int bytesRead = await _stream.ReadAsync(buffer.AsMemory(totalRead, count - totalRead));
			if (bytesRead == 0) return false; // Disconnected
			totalRead += bytesRead;
		}
		return true;
	}

	private void OnMessageRecv(IDataServerMessage msg)
	{
		if (msg is MessageAuthRes)
		{
			_authTcs?.TrySetResult();
			_authTcs = null;
			return;
		}
		MessageReceived?.Invoke(msg);
	}

	public async Task SendAuthenticate(string token, int peerID)
	{
		_authTcs = new();
		await SendMessage(new MessageAuthenticate() { AuthToken = token, PeerID = peerID });

		// Wait for server's response
		await _authTcs.Task;
	}

	public async Task SendMessage(IDataServerMessage msg)
	{
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
}
