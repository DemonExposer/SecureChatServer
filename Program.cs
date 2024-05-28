using System.Data.SQLite;

string connstr = "Data Source=MyDatabase.sqlite;Version=3;";
using (SQLiteConnection connection = new SQLiteConnection(connstr)) {
	connection.Open();

	SQLiteCommand command = connection.CreateCommand();
	command.CommandText = "CREATE TABLE IF NOT EXISTS messages (id INTEGER PRIMARY KEY, body TEXT, User INTEGER);";

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

app.UseAuthorization();

app.MapControllers();

app.Run();