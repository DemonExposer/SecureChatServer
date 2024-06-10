using System.Collections.Concurrent;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;

namespace SecureChatServer.Controllers;

[ApiController]
public class WebSocketController : ControllerBase {
	public static readonly ConcurrentDictionary<string, WebSocketHandler> Sockets = new ();

	[HttpGet("ws")]
	public async Task Get() {
		if (HttpContext.WebSockets.IsWebSocketRequest) {
			WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
			WebSocketHandler webSocketHandler = new (webSocket);
			await webSocketHandler.Handle();
		} else {
			HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
		}
	}
}
