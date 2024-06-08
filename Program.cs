using System.Data.SQLite;

using (SQLiteConnection connection = new (SecureChatServer.Constants.DbConnectionString)) {
	connection.Open();

	SQLiteCommand command = connection.CreateCommand();
	command.CommandText = @"
		CREATE TABLE IF NOT EXISTS messages (id INTEGER PRIMARY KEY, body TEXT, sender INTEGER, receiver INTEGER, sender_encrypted_key TEXT, receiver_encrypted_key TEXT, signature TEXT);
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

app.UseHttpsRedirection();

app.UseWebSockets();

app.UseAuthorization();

app.MapControllers();

app.Run();