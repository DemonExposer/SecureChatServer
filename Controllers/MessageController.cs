using System.Data.SQLite;
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
		string connstr = "Data Source=MyDatabase.sqlite;Version=3;";
		using (SQLiteConnection connection = new (connstr)) {
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