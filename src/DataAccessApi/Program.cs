using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// MongoDB client (még nem használjuk CRUD-ra, csak bekötjük)
builder.Services.AddSingleton<IMongoClient>(_ =>
{
	var cs = builder.Configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
	return new MongoClient(cs);
});

var app = builder.Build();

// Egyszerű health endpoint
app.MapGet("/health", () => Results.Ok(new { ok = true, service = "dataaccess" }));

app.Run();