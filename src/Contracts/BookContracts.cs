using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts
{
	public record BookDto(string Id, string Title, string Author, int Year, string Status);

	public record CreateBookRequest(string Title, string Author, int Year, string Status);

	public record UpdateBookRequest(string Title, string Author, int Year, string Status);

	public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long Total);
}
