using System.Net.Http.Json;
using Contracts;

namespace WebApi;

public interface IDataAccessClient
{
	Task<PagedResult<BookDto>> GetBooks(int page, int pageSize, CancellationToken ct);
	Task<BookDto?> GetBook(string id, CancellationToken ct);
	Task<BookDto> CreateBook(CreateBookRequest req, CancellationToken ct);
	Task<bool> UpdateBook(string id, UpdateBookRequest req, CancellationToken ct);
	Task<bool> DeleteBook(string id, CancellationToken ct);
}

public sealed class DataAccessClient(HttpClient http) : IDataAccessClient
{
	public async Task<PagedResult<BookDto>> GetBooks(int page, int pageSize, CancellationToken ct)
		=> (await http.GetFromJsonAsync<PagedResult<BookDto>>($"/books?page={page}&pageSize={pageSize}", ct))!;

	public Task<BookDto?> GetBook(string id, CancellationToken ct)
		=> http.GetFromJsonAsync<BookDto>($"/books/{id}", ct);

	public async Task<BookDto> CreateBook(CreateBookRequest req, CancellationToken ct)
	{
		var res = await http.PostAsJsonAsync("/books", req, ct);
		res.EnsureSuccessStatusCode();
		return (await res.Content.ReadFromJsonAsync<BookDto>(cancellationToken: ct))!;
	}

	public async Task<bool> UpdateBook(string id, UpdateBookRequest req, CancellationToken ct)
		=> (await http.PutAsJsonAsync($"/books/{id}", req, ct)).IsSuccessStatusCode;

	public async Task<bool> DeleteBook(string id, CancellationToken ct)
		=> (await http.DeleteAsync($"/books/{id}", ct)).IsSuccessStatusCode;
}