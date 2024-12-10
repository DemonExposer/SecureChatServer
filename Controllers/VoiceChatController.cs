using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Crypto.Parameters;

namespace SecureChatServer.Controllers;

[ApiController]
[Route("voiceChat")]
public class VoiceChatController : ControllerBase {
	[HttpPost]
	public ActionResult Post(string personalModulus, string foreignModulus, string encryptedKeyBase64, long timestamp) {
		// TODO: Implement adding connection to VoiceChats, verifying the request and sending back the key in case the other party started the voice chat
		throw new NotImplementedException();
	}
}