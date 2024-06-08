using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace SecureChatServer.Controllers;

[ApiController]
public class WebSocketController : ControllerBase {
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
		var buffer = new byte[1024];

		while (true) {
			try {
				await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("hello")), WebSocketMessageType.Text, true, CancellationToken.None);
			} catch (Exception) {
				return;
			}

			Thread.Sleep(1000);
		}
	}
}
