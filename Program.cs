using System.Data.SQLite;
using System.Net;
using System.Net.Sockets;
using System.Text;

using (SQLiteConnection connection = new (SecureChatServer.Constants.DbConnectionString)) {
	connection.Open();

	SQLiteCommand command = connection.CreateCommand();
	command.CommandText = @"
		CREATE TABLE IF NOT EXISTS messages (id INTEGER PRIMARY KEY, body TEXT, sender INTEGER, receiver INTEGER, sender_encrypted_key TEXT, receiver_encrypted_key TEXT, signature TEXT, is_read INTEGER DEFAULT 0);
		CREATE TABLE IF NOT EXISTS users (id INTEGER PRIMARY KEY, modulus TEXT UNIQUE, exponent TEXT);
	";

	command.ExecuteNonQuery();
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.Use((context, next) => {
	context.Request.EnableBuffering();
	return next();
});

app.UseHttpsRedirection();

app.UseWebSockets();

app.UseAuthorization();

app.MapControllers();

UdpClient socket = new (5001);
Dictionary<IPEndPoint, VoiceClient> connections = new ();
Dictionary<string, IPEndPoint> addresses = new ();
// TODO: encrypt, verify and make more efficient
Task.Run(() => {
	while (true) try {
		IPEndPoint endPoint = new (IPAddress.Any, 5001);
		byte[] data = socket.Receive(ref endPoint);
		if (connections.TryGetValue(endPoint, out VoiceClient voiceClient)) {
			if (addresses.ContainsKey(voiceClient.ForeignModulus))
				socket.Send(data, data.Length, addresses[voiceClient.ForeignModulus]);
		} else {
			string[] message = Encoding.UTF8.GetString(data).Split("-");
			connections[endPoint] = new VoiceClient { PersonalModulus = message[0], ForeignModulus = message[1] };
			addresses[message[0]] = endPoint;
		}
	} catch (Exception e) {
		Console.WriteLine(e);
	}
});

app.Run();

class VoiceClient {
	public string PersonalModulus { get; set; }
	public string ForeignModulus { get; set; }
}