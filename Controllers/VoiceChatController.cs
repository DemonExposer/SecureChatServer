using System.Net;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using SecureChatServer.util;

namespace SecureChatServer.Controllers;

[ApiController]
[Route("voiceChat")]
public class VoiceChatController : ControllerBase {
	[HttpPut]
	// IP might not be retrievable automatically, because the server is designed to run with a proxy
	// TODO: the client should find out about their ip and port by sending a request over UDP (make this reliable with a hash or something similar), where the server sends that data back
	public ActionResult Put(string personalModulus, string foreignModulus, string encryptedKeyBase64, string ip, int port, long timestamp) {
		throw new NotImplementedException();
		RsaKeyParameters personalKey = new (false, new BigInteger(personalModulus, 16), new BigInteger("10001", 16));
		RsaKeyParameters foreignKey = new (false, new BigInteger(foreignModulus, 16), new BigInteger("10001", 16));
		if (VoiceChats.Exists(personalKey))
			return Conflict(); // Consider just sending the result back instead
		if (VoiceChats.Exists(foreignKey)) {
			VoiceChats.AddForeignEndpoint(new IPEndPoint(IPAddress.Parse(ip), port), personalKey);
		} else {
			// TODO: Implement adding connection to VoiceChats, verifying the request and sending back the key in case the other party started the voice chat
		}
	}
}