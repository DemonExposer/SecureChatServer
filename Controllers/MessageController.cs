using System.Data.SQLite;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using SecureChatServer.util;

namespace SecureChatServer.Controllers;

[ApiController]
[Route("messages")]
public class MessageController : ControllerBase {
	public class DeleteRequestBody {
		public long Id { get; set; }
		public string Signature { get; set; }
	}
	
	[HttpGet]
	public Message[] Get(string requestingUserModulus, string requestingUserExponent, string requestedUserModulus, string requestedUserExponent) {
		List<Message> res = new ();
		using (SQLiteConnection connection = new (Constants.DbConnectionString)) {
			connection.Open();

			SQLiteCommand command = connection.CreateCommand();
			command.CommandText = "SELECT * FROM users WHERE modulus = @modulus1 AND exponent = @exponent1 OR modulus = @modulus2 AND exponent = @exponent2;";
			command.Parameters.AddWithValue("@modulus1", requestingUserModulus);
			command.Parameters.AddWithValue("@exponent1", requestingUserExponent);
			command.Parameters.AddWithValue("@modulus2", requestedUserModulus);
			command.Parameters.AddWithValue("@exponent2", requestedUserExponent);

			long requestingUserId = -1, requestedUserId = -1;
			using (SQLiteDataReader reader = command.ExecuteReader())
				while (reader.Read())
					if ((string) reader["modulus"] == requestingUserModulus)
						requestingUserId = (long) reader["id"];
					else if ((string) reader["modulus"] == requestedUserModulus)
						requestedUserId = (long) reader["id"];

			if (requestingUserId == -1 || requestedUserId == -1)
				return res.ToArray();

			command.Parameters.Clear();

			command.CommandText = "SELECT * FROM messages WHERE sender = @requesterId AND receiver = @requestedId OR sender = @requestedId AND receiver = @requesterId;";
			command.Parameters.AddWithValue("@requesterId", requestingUserId);
			command.Parameters.AddWithValue("@requestedId", requestedUserId);

			using (SQLiteDataReader reader = command.ExecuteReader()) {
				while (reader.Read()) {
					string senderModulus, senderExponent, receiverModulus, receiverExponent;
					if ((long) reader["sender"] == requestingUserId) {
						senderModulus = requestingUserModulus;
						senderExponent = requestingUserExponent;
						receiverModulus = requestedUserModulus;
						receiverExponent = requestedUserExponent;
					} else {
						senderModulus = requestedUserModulus;
						senderExponent = requestedUserExponent;
						receiverModulus = requestingUserModulus;
						receiverExponent = requestingUserExponent;
					}

					res.Add(
						new Message {
							Id = (long) reader["id"],
							DateTime = DateTime.Now,
							Text = (string) reader["body"],
							ReceiverEncryptedKey = (string) reader["receiver_encrypted_key"],
							SenderEncryptedKey = (string) reader["sender_encrypted_key"],
							Signature = (string) reader["signature"],
							Sender = new User { Modulus = senderModulus, Exponent = senderExponent },
							Receiver = new User { Modulus = receiverModulus, Exponent = receiverExponent }
						}
					);
				}
			}
		}

		return res.ToArray();
	}

	[HttpPost]
	public long Post(Message message) {
		using SQLiteConnection connection = new (Constants.DbConnectionString);
		connection.Open();

		SQLiteCommand command = connection.CreateCommand();
		command.CommandText = "INSERT OR IGNORE INTO users (modulus, exponent) VALUES (@modulus, @exponent);";
		command.Parameters.AddWithValue("@modulus", message.Sender.Modulus);
		command.Parameters.AddWithValue("@exponent", message.Sender.Exponent);
		command.ExecuteNonQuery();

		command.Parameters.Clear();

		command.CommandText = "INSERT OR IGNORE INTO users (modulus, exponent) VALUES (@modulus, @exponent);";
		command.Parameters.AddWithValue("@modulus", message.Receiver.Modulus);
		command.Parameters.AddWithValue("@exponent", message.Receiver.Exponent);
		command.ExecuteNonQuery();

		command.Parameters.Clear();

		command.CommandText = "SELECT id FROM users WHERE modulus = @modulus AND exponent = @exponent;";
		command.Parameters.AddWithValue("@modulus", message.Sender.Modulus);
		command.Parameters.AddWithValue("@exponent", message.Sender.Exponent);
		long senderId;
		using (SQLiteDataReader reader = command.ExecuteReader()) {
			reader.Read();
			senderId = (long) reader["id"];
		}

		command.Parameters.Clear();

		command.CommandText = "SELECT id FROM users WHERE modulus = @modulus AND exponent = @exponent;";
		command.Parameters.AddWithValue("@modulus", message.Receiver.Modulus);
		command.Parameters.AddWithValue("@exponent", message.Receiver.Exponent);
		long receiverId;
		using (SQLiteDataReader reader = command.ExecuteReader()) {
			reader.Read();
			receiverId = (long) reader["id"];
		}

		command.Parameters.Clear();

		command.CommandText = "INSERT INTO messages (body, sender, receiver, sender_encrypted_key, receiver_encrypted_key, signature) VALUES (@body, @sender, @receiver, @senderEncKey, @receiverEncKey, @signature);";
		command.Parameters.AddWithValue("@body", message.Text);
		command.Parameters.AddWithValue("@sender", senderId);
		command.Parameters.AddWithValue("@receiver", receiverId);
		command.Parameters.AddWithValue("@senderEncKey", message.SenderEncryptedKey);
		command.Parameters.AddWithValue("@receiverEncKey", message.ReceiverEncryptedKey);
		command.Parameters.AddWithValue("@signature", message.Signature);
		command.ExecuteNonQuery();
		
		command.Parameters.Clear();

		command.CommandText = "SELECT last_insert_rowid() AS id;";
		long messageId;
		using (SQLiteDataReader reader = command.ExecuteReader()) {
			reader.Read();
			messageId = (long) reader["id"];
		}

		bool webSocketExists = WebSocketController.Sockets.TryGetValue(message.Receiver.Modulus, out WebSocketHandler? webSocketHandler);
		if (!webSocketExists || webSocketHandler == null)
			return messageId;

		// TODO: remove the websocket if the send fails
		webSocketHandler.Message = message;
		webSocketHandler.ManualResetEvent.Set();

		return messageId;
	}

	[HttpDelete]
	public void Delete(DeleteRequestBody body) {
		using SQLiteConnection connection = new (Constants.DbConnectionString);
		connection.Open();

		SQLiteCommand command = connection.CreateCommand();
		command.CommandText = "SELECT messages.`id`, sender, users.`id`, modulus, exponent FROM messages LEFT JOIN users ON sender = users.`id` WHERE messages.`id` = @id;";
		command.Parameters.AddWithValue("@id", body.Id);
		
		string modulus, exponent;
		using (SQLiteDataReader reader = command.ExecuteReader()) {
			// TODO: reject when .Read() returns false
			reader.Read();
			modulus = (string) reader["modulus"];
			exponent = (string) reader["exponent"];
		}

		command.Parameters.Clear();
		
		// TODO: maybe send a timestamp as well to prevent replay attacks
		if (!Cryptography.Verify(body.Id.ToString(), body.Signature, new RsaKeyParameters(false, new BigInteger(modulus, 16), new BigInteger(exponent, 16)))) {
			return; // TODO: give an error code
		}

		command.CommandText = "DELETE FROM messages WHERE `id` = @id;";
		command.Parameters.AddWithValue("@id", body.Id);
		command.ExecuteNonQuery();
	}
}