using SecureChatServer.Controllers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace SecureChatServer;

public class WebSocketHandler {
	private readonly WebSocket _webSocket;

	public readonly ManualResetEvent ManualResetEvent = new (false);

	public Message Message;

	public WebSocketHandler(WebSocket webSocket) {
		_webSocket = webSocket;
	}

	public async Task Handle() {
		List<byte> bytes = new ();
		int arrSize = 1024;
		byte[] buffer = new byte[arrSize];

		WebSocketReceiveResult result = null;
		do {
			try {
				result = await _webSocket.ReceiveAsync(buffer, CancellationToken.None);
			} catch (Exception e) {
				Console.WriteLine(e.ToString());
			}
			bytes.AddRange(buffer[..result.Count]);
		} while (result.Count == arrSize);

		string modulus = Encoding.UTF8.GetString(bytes.ToArray(), 0, bytes.Count);
		WebSocketController.Sockets[modulus] = this;

		while (true) {
			ManualResetEvent.WaitOne();
			try {
				await _webSocket.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new JsonObject {
					["signature"] = Message.Signature,
					["sender"] = new JsonObject {
						["modulus"] = Message.Sender.Modulus,
						["exponent"] = Message.Sender.Exponent
					},
					["text"] = Message.Text,
					["receiverEncryptedKey"] = Message.ReceiverEncryptedKey
				})), WebSocketMessageType.Text, true, CancellationToken.None);
			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}
			ManualResetEvent.Reset();
		}
	}

	public async Task Send(Message message) {
		try {
			await _webSocket.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new JsonObject {
				["signature"] = message.Signature,
				["sender"] = new JsonObject {
					["modulus"] = message.Sender.Modulus,
					["exponent"] = message.Sender.Exponent
				},
				["text"] = message.Text,
				["receiverEncryptedKey"] = message.ReceiverEncryptedKey
			})), WebSocketMessageType.Text, true, CancellationToken.None);
		} catch (Exception ex) {
			Console.WriteLine(ex.ToString());
		}
	}
}