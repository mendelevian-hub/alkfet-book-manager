using Contracts;
using WebApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi(); 

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
	p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var baseUrl = builder.Configuration["DataAccess:BaseUrl"] ?? "http://localhost:5033";
builder.Services.AddHttpClient<IDataAccessClient, DataAccessClient>(c => c.BaseAddress = new Uri(baseUrl));

var app = builder.Build();
if (app.Environment.IsDevelopment()) app.MapOpenApi(); 
app.UseCors();

app.MapGet("/api/books", async (int page, int pageSize, IDataAccessClient client, CancellationToken ct)
	=> Results.Ok(await client.GetBooks(page, pageSize, ct)));

app.MapGet("/api/books/{id}", async (string id, IDataAccessClient client, CancellationToken ct)
	=> (await client.GetBook(id, ct)) is { } b ? Results.Ok(b) : Results.NotFound());

app.MapPost("/api/books", async (CreateBookRequest req, IDataAccessClient client, CancellationToken ct)
	=> Results.Created("", await client.CreateBook(req, ct)));

app.MapPut("/api/books/{id}", async (string id, UpdateBookRequest req, IDataAccessClient client, CancellationToken ct)
	=> (await client.UpdateBook(id, req, ct)) ? Results.NoContent() : Results.NotFound());

app.MapDelete("/api/books/{id}", async (string id, IDataAccessClient client, CancellationToken ct)
	=> (await client.DeleteBook(id, ct)) ? Results.NoContent() : Results.NotFound());

app.Run();