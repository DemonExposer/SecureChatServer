using System.Data.SQLite;
using System.Net;
using System.Net.Sockets;
using SecureChatServer.util;

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
// TODO: encrypt, verify and make more efficient
Task.Run(() => {
	while (true) try {
		IPEndPoint endPoint = new (IPAddress.Any, 5001);
		byte[] data = socket.Receive(ref endPoint);
		if (VoiceChats.TryGet(endPoint, out IPEndPoint? foreignEndPoint))
			socket.Send(data, data.Length, foreignEndPoint);
	} catch (Exception e) {
		Console.WriteLine(e);
	}
});

app.Run();
