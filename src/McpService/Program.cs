using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

// MCP server + HTTP transport + tool discovery (assembly scan)
builder.Services.AddMcpServer()
	.WithHttpTransport()
	.WithToolsFromAssembly(); // később megtalálja a [McpServerToolType] osztályokat

// (Ajánlott) CORS – több MCP kliens böngészőből/extensionből hív
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
	p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// DataAccessApi HTTP kliens
var baseUrl = builder.Configuration["DataAccess:BaseUrl"] ?? "http://localhost:5033";
builder.Services.AddHttpClient("dataaccess", c => c.BaseAddress = new Uri(baseUrl));
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("dataaccess"));

var app = builder.Build();
app.UseCors();

app.MapGet("/health", () => Results.Ok(new { ok = true, service = "mcp" }));

// Opcionális API-key védelem: ha AuthKey üres -> nincs ellenőrzés
var expectedKey = app.Configuration["Mcp:AuthKey"];
app.Use(async (ctx, next) =>
{
	if (ctx.Request.Path.StartsWithSegments("/mcp") && !string.IsNullOrWhiteSpace(expectedKey))
	{
		var got = ctx.Request.Headers["X-MCP-KEY"].ToString();
		if (got != expectedKey)
		{
			ctx.Response.StatusCode = 401;
			await ctx.Response.WriteAsync("Unauthorized");
			return;
		}
	}
	await next();
});

// MCP endpoint: /mcp
app.MapMcp("/mcp"); // streamable HTTP MCP endpoint

app.Run();