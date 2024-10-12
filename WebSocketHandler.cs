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

	public enum MessageAction {
		Add,
		Delete,
		Invalid
	}
	
	public MessageAction Action = MessageAction.Invalid;

	public WebSocketHandler(WebSocket webSocket) {
		_webSocket = webSocket;
	}

	public async Task Handle() {
		List<byte> bytes = new ();
		int arrSize = 1024;
		byte[] buffer = new byte[arrSize];

		WebSocketReceiveResult? result = null;
		do {
			try {
				result = await _webSocket.ReceiveAsync(buffer, CancellationToken.None);
			} catch (Exception e) {
				Console.WriteLine(e.ToString()); // TODO: handle this differently
				return;
			}
			bytes.AddRange(buffer[..result.Count]);
		} while (result.Count == arrSize);

		string modulus = Encoding.UTF8.GetString(bytes.ToArray(), 0, bytes.Count);
		WebSocketController.Sockets[modulus] = this;

		// Imagine Microsoft making a good library... sadly this is the only way to make the websocket work
		while (true) {
			ManualResetEvent.WaitOne();
			try {
				JsonObject? messageObj = null;
				switch (Action) {
					case MessageAction.Add:
						messageObj = new JsonObject {
							["action"] = "add",
							["id"] = Message.Id,
							["signature"] = Message.Signature,
							["sender"] = new JsonObject {
								["modulus"] = Message.Sender.Modulus,
								["exponent"] = Message.Sender.Exponent
							},
							["text"] = Message.Text,
							["receiverEncryptedKey"] = Message.ReceiverEncryptedKey,
							["isRead"] = Message.IsRead
						};
						break;
					case MessageAction.Delete:
						messageObj = new JsonObject {
							["action"] = "delete",
							["id"] = Message.Id,
							["sender"] = new JsonObject {
								["modulus"] = Message.Sender.Modulus,
								["exponent"] = Message.Sender.Exponent
							}
						};
						break;
					case MessageAction.Invalid:
						throw new InvalidOperationException("Websocket action was not specified");
				}
				
				await _webSocket.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(messageObj)), WebSocketMessageType.Text, true, CancellationToken.None);
			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}

			Action = MessageAction.Invalid;
			ManualResetEvent.Reset();
		}
	}
}