
using Contracts;
using DataAccessApi;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IMongoClient>(_ =>
{
	var cs = builder.Configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
	return new MongoClient(cs);
});
builder.Services.AddSingleton<IBooksRepository, BooksRepository>();

var app = builder.Build();

// Egyszerű health endpoint
app.MapGet("/health", () => Results.Ok(new { ok = true, service = "dataaccess" }));

app.MapGet("/books", async (IBooksRepository repo, CancellationToken ct, int page = 1, int pageSize = 10)
    => Results.Ok(await repo.GetPaged(page, pageSize, ct)));

app.MapGet("/books/{id}", async (string id, IBooksRepository repo, CancellationToken ct) =>
{
	var book = await repo.GetById(id, ct);
	return book is null ? Results.NotFound() : Results.Ok(book);
});

app.MapPost("/books", async (CreateBookRequest req, IBooksRepository repo, CancellationToken ct) =>
{
	var created = await repo.Create(req, ct);
	return Results.Created($"/books/{created.Id}", created);
});

app.MapPut("/books/{id}", async (string id, UpdateBookRequest req, IBooksRepository repo, CancellationToken ct) =>
	(await repo.Update(id, req, ct)) ? Results.NoContent() : Results.NotFound());

app.MapDelete("/books/{id}", async (string id, IBooksRepository repo, CancellationToken ct) =>
	(await repo.Delete(id, ct)) ? Results.NoContent() : Results.NotFound());


app.Run();