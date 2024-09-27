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
	public Chat[] Get(string modulus, string exponent, long timestamp) {
		List<Chat> result = new ();
		
		if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - timestamp > 10000) {
			return result.ToArray(); // TODO: give an error code
		}

		string signature = Request.Headers["Signature"].ToString();
		string queryString = Request.QueryString.Value![1..];
		if (!Cryptography.Verify(queryString, signature, new RsaKeyParameters(false, new BigInteger(modulus, 16), new BigInteger(exponent, 16)))) {
			return result.ToArray(); // TODO: give an error code
		}
		
		using SQLiteConnection connection = new (Constants.DbConnectionString);
		connection.Open();

		SQLiteCommand command = connection.CreateCommand();
		command.CommandText = "SELECT DISTINCT u2.modulus, u2.exponent FROM messages JOIN users u1 ON sender = u1.id OR receiver = u1.id JOIN users u2 ON sender = u2.id OR receiver = u2.id WHERE u1.modulus = @modulus AND u1.exponent = @exponent AND u1.id <> u2.id;";
		command.Parameters.AddWithValue("@modulus", modulus);
		command.Parameters.AddWithValue("@exponent", exponent);
		
		using (SQLiteDataReader reader = command.ExecuteReader()) {
			while (reader.Read()) {
				result.Add(new Chat {
					User1 = new User {
						Modulus = modulus,
						Exponent = exponent
					},
					User2 = new User {
						Modulus = (string) reader["modulus"],
						Exponent = (string) reader["exponent"]
					}
				});
			}
		}

		return result.ToArray();
	}
}