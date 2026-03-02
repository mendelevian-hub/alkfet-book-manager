using System.ComponentModel;
using System.Net.Http.Json;
using Contracts;
using ModelContextProtocol.Server;

namespace McpService.Tools;

[McpServerToolType]
public class BooksMcpTools
{
	private readonly HttpClient _http;

	public BooksMcpTools(HttpClient http)
	{
		_http = http; // ezt a Program.cs-ben regisztrált dataaccess HttpClient adja
	}

	[McpServerTool, Description("List books (paged).")]
	public async Task<PagedResult<BookDto>> GetBooks(int page, int pageSize, CancellationToken ct)
		=> (await _http.GetFromJsonAsync<PagedResult<BookDto>>($"/books?page={page}&pageSize={pageSize}", ct))!;

	[McpServerTool, Description("Create a book.")]
	public async Task<BookDto> CreateBook(string title, string author, int year, string status, CancellationToken ct)
	{
		var res = await _http.PostAsJsonAsync("/books",
			new CreateBookRequest(title, author, year, status), ct);

		res.EnsureSuccessStatusCode();
		return (await res.Content.ReadFromJsonAsync<BookDto>(cancellationToken: ct))!;
	}

	[McpServerTool, Description("Delete a book by id.")]
	public async Task<bool> DeleteBook(string id, CancellationToken ct)
		=> (await _http.DeleteAsync($"/books/{id}", ct)).IsSuccessStatusCode;
}