using System.Data.SQLite;
using Microsoft.AspNetCore.Mvc;

namespace SecureChatServer.Controllers;

[ApiController]
[Route("messages")]
public class MessageController : ControllerBase {
	[HttpGet]
	public Message[] Get(string modulus, string exponent) {
		using (SQLiteConnection connection = new (Constants.DbConnectionString)) {
			connection.Open();

			SQLiteCommand command = connection.CreateCommand();
			command.CommandText = "";
		}
		return new[] { new Message { DateTime = DateTime.Now, Text = "hello", User = new User { Modulus = "123", Exponent = "123"} } };
	}

	[HttpPost]
	public void Post(Message message) {
		using (SQLiteConnection connection = new (Constants.DbConnectionString)) {
			connection.Open();

			SQLiteCommand command = connection.CreateCommand();
			command.CommandText = "INSERT INTO messages (body, user) VALUES (@body, @user);";
			command.Parameters.AddWithValue("@body", message.Text);
			command.Parameters.AddWithValue("@user", 1);
			command.ExecuteNonQuery();

			command.Parameters.Clear();

			command.CommandText = "SELECT * FROM messages;";
			using SQLiteDataReader reader = command.ExecuteReader();
			while (reader.Read())
				Console.WriteLine($"{reader["id"]}: {reader["body"]}");
		}
	}
}