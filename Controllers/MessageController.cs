using Microsoft.AspNetCore.Mvc;

namespace SecureChatServer.Controllers;

[ApiController]
[Route("messages")]
public class MessageController : ControllerBase {
	[HttpGet]
	public Message[] Get() {
		return new[] { new Message { DateTime = DateTime.Now, Text = "hello", User = new User { Modulus = "123", Exponent = "123"} } };
	}

	[HttpPost]
	public void Post(Message message) {
		Console.WriteLine(message.Text);
		Console.WriteLine(message.User.Modulus);
		Console.WriteLine(message.User.Exponent);
	}
}