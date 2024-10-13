using System.Data.SQLite;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using SecureChatServer.util;

namespace SecureChatServer.Controllers;

[ApiController]
[Route("makeChatRead")]
public class MakeChatReadController : ControllerBase {
	public class MakeChatReadObject {
		public User Receiver { get; set; }
	
		public User Sender { get; set; }
		
		public long Timestamp { get; set; }
	}

	[HttpPost]
	public async Task Post(MakeChatReadObject data) {
		if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - data.Timestamp > 10000) {
			return; // TODO: give an error code
		}

		Request.Body.Position = 0;
		
		string signature = Request.Headers["Signature"].ToString();
		string requestBody;
		using (StreamReader reader = new (Request.Body)) {
			requestBody = await reader.ReadToEndAsync();
			Request.Body.Position = 0;
		}
		if (!Cryptography.Verify(requestBody, signature, new RsaKeyParameters(false, new BigInteger(data.Receiver.Modulus, 16), new BigInteger(data.Receiver.Exponent, 16)))) {
			return; // TODO: give an error code
		}

		await using SQLiteConnection connection = new (Constants.DbConnectionString);
		connection.Open();

		SQLiteCommand command = connection.CreateCommand();

		command.CommandText =
			"UPDATE messages SET is_read = 1 WHERE is_read = 0 AND sender = (SELECT DISTINCT sender FROM messages JOIN users u1 ON receiver = u1.id JOIN users u2 ON sender = u2.id WHERE u1.modulus = @personalModulus AND u1.exponent = @personalExponent AND u2.modulus = @foreignModulus AND u2.exponent = @foreignExponent) AND receiver = (SELECT DISTINCT receiver FROM messages JOIN users u1 ON receiver = u1.id JOIN users u2 ON sender = u2.id WHERE u1.modulus = @personalModulus AND u1.exponent = @personalExponent AND u2.modulus = @foreignModulus AND u2.exponent = @foreignExponent);";
		command.Parameters.AddWithValue("@personalModulus", data.Receiver.Modulus);
		command.Parameters.AddWithValue("@personalExponent", data.Receiver.Exponent);
		command.Parameters.AddWithValue("@foreignModulus", data.Sender.Modulus);
		command.Parameters.AddWithValue("@foreignExponent", data.Sender.Exponent);
		
		command.ExecuteNonQuery();
	}
}