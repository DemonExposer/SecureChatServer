using System.Data.SQLite;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using SecureChatServer.util;

namespace SecureChatServer.Controllers;

[ApiController]
[Route("chats")]
public class ChatController : ControllerBase {
	[HttpGet]
	public ActionResult<Chat[]> Get(string modulus, string exponent, long timestamp) {
		List<Chat> result = new ();
		
		if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - timestamp > 10000) {
			return Unauthorized(result.ToArray()); // TODO: specify that it's a timestamp issue
		}

		string signature = Request.Headers["Signature"].ToString();
		string queryString = Request.QueryString.Value![1..];
		if (!Cryptography.Verify(queryString, signature, new RsaKeyParameters(false, new BigInteger(modulus, 16), new BigInteger(exponent, 16)))) {
			return Unauthorized(result.ToArray());
		}
		
		using SQLiteConnection connection = new (Constants.DbConnectionString);
		connection.Open();

		SQLiteCommand command = connection.CreateCommand();
		command.CommandText = "SELECT DISTINCT u2.modulus, u2.exponent FROM messages JOIN users u1 ON sender = u1.id OR receiver = u1.id JOIN users u2 ON sender = u2.id OR receiver = u2.id WHERE u1.modulus = @modulus AND u1.exponent = @exponent AND u1.id <> u2.id;";
		command.Parameters.AddWithValue("@modulus", modulus);
		command.Parameters.AddWithValue("@exponent", exponent);
		
		SQLiteCommand command2 = connection.CreateCommand();
		using (SQLiteDataReader reader = command.ExecuteReader()) {
			while (reader.Read()) {
				string foreignModulus = (string) reader["modulus"];
				string foreignExponent = (string) reader["exponent"];
				
				command2.CommandText = "SELECT COUNT() AS count FROM messages JOIN users u1 ON receiver = u1.id JOIN users u2 ON sender = u2.id WHERE is_read = 0 AND u1.modulus = @personalModulus AND u1.exponent = @personalExponent AND u2.modulus = @foreignModulus AND u2.exponent = @foreignExponent;";
				command2.Parameters.AddWithValue("@personalModulus", modulus);
				command2.Parameters.AddWithValue("@personalExponent", exponent);
				command2.Parameters.AddWithValue("@foreignModulus", foreignModulus);
				command2.Parameters.AddWithValue("@foreignExponent", foreignExponent);

				bool isChatRead;
				using (SQLiteDataReader reader2 = command2.ExecuteReader()) {
					reader2.Read();
					isChatRead = (long) reader2["count"] == 0;
				}
				
				result.Add(new Chat {
					User1 = new User {
						Modulus = modulus,
						Exponent = exponent
					},
					User2 = new User {
						Modulus = foreignModulus,
						Exponent = foreignExponent
					},
					IsRead = isChatRead
				});
			}
		}

		return Ok(result.ToArray());
	}
}