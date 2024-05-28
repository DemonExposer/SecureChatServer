using System.Data.SQLite;
using Microsoft.AspNetCore.Mvc;

namespace SecureChatServer.Controllers;

[ApiController]
[Route("messages")]
public class MessageController : ControllerBase {
	[HttpGet]
	public Message[] Get(string modulus, string exponent) {
		List<Message> res = new ();
		using (SQLiteConnection connection = new (Constants.DbConnectionString)) {
			connection.Open();

			SQLiteCommand command = connection.CreateCommand();
			command.CommandText = "SELECT * FROM messages LEFT JOIN users ON messages.user = users.id WHERE users.modulus = @modulus AND users.exponent = @exponent;";
			command.Parameters.AddWithValue("@modulus", modulus);
			command.Parameters.AddWithValue("@exponent", exponent);

			using SQLiteDataReader reader = command.ExecuteReader();

			while (reader.Read())
				res.Add(new Message { DateTime = DateTime.Now, Text = (string) reader["body"], User = new User { Modulus = (string) reader["modulus"], Exponent = (string) reader["exponent"] } });
		}

		return res.ToArray();
	}

	[HttpPost]
	public void Post(Message message) {
		using SQLiteConnection connection = new (Constants.DbConnectionString);
		connection.Open();

		SQLiteCommand command = connection.CreateCommand();
		command.CommandText = "INSERT OR IGNORE INTO users (modulus, exponent) VALUES (@modulus, @exponent);";
		command.Parameters.AddWithValue("@modulus", message.User.Modulus);
		command.Parameters.AddWithValue("@exponent", message.User.Exponent);
		command.ExecuteNonQuery();

		command.Parameters.Clear();

		command.CommandText = "INSERT INTO messages (body, user) SELECT @body, id FROM users WHERE modulus = @modulus AND exponent = @exponent;";
		command.Parameters.AddWithValue("@body", message.Text);
		command.Parameters.AddWithValue("@modulus", message.User.Modulus);
		command.Parameters.AddWithValue("@exponent", message.User.Exponent);
		command.ExecuteNonQuery();
	}
}