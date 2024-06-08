using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace SecureChatServer.Controllers;

[ApiController]
public class WebSocketController : ControllerBase {
	public static readonly IDictionary<string, WebSocket> Sockets = new Dictionary<string, WebSocket>();

	[Route("ws")]
	public async Task Get() {
		if (HttpContext.WebSockets.IsWebSocketRequest) {
			using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
			await HandleWebSocket(webSocket);
		} else {
			HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
		}
	}

	private static async Task HandleWebSocket(WebSocket webSocket) {
		List<byte> bytes = new ();
		int arrSize = 1024;
		byte[] buffer = new byte[arrSize];

		WebSocketReceiveResult result;
		do {
			result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
			bytes.AddRange(buffer[..result.Count]);
		} while (result.Count == arrSize);

		string modulus = Encoding.UTF8.GetString(bytes.ToArray(), 0, bytes.Count);
		Sockets[modulus] = webSocket;
	}
}
