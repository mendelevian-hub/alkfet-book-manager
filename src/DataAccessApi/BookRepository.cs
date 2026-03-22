using Contracts;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace DataAccessApi;

public class BookDocument
{
	[BsonId]
	[BsonRepresentation(BsonType.ObjectId)]
	public string Id { get; set; } = null!;

	public string Title { get; set; } = "";
	public string Author { get; set; } = "";
	public int Year { get; set; }
	public string Status { get; set; } = "Planned";
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public interface IBooksRepository
{
	Task<PagedResult<BookDto>> GetPaged(int page, int pageSize, CancellationToken ct);
	Task<BookDto?> GetById(string id, CancellationToken ct);
	Task<BookDto> Create(CreateBookRequest req, CancellationToken ct);
	Task<bool> Update(string id, UpdateBookRequest req, CancellationToken ct);
	Task<bool> Delete(string id, CancellationToken ct);
}

public sealed class BooksRepository : IBooksRepository
{
	private readonly IMongoCollection<BookDocument> _col;

	public BooksRepository(IMongoClient client, IConfiguration cfg)
	{
		var db = client.GetDatabase(cfg["Mongo:Database"] ?? "alkfet");
		_col = db.GetCollection<BookDocument>("books");
	}

	public async Task<PagedResult<BookDto>> GetPaged(int page, int pageSize, CancellationToken ct)
	{
		page = Math.Max(1, page);
		pageSize = Math.Clamp(pageSize, 1, 100);

		var total = await _col.CountDocumentsAsync(FilterDefinition<BookDocument>.Empty, cancellationToken: ct);
		var items = await _col.Find(FilterDefinition<BookDocument>.Empty)
			.SortByDescending(x => x.CreatedAt)
			.Skip((page - 1) * pageSize)
			.Limit(pageSize)
			.ToListAsync(ct);

		return new(items.Select(ToDto).ToList(), page, pageSize, total);
	}

	public async Task<BookDto?> GetById(string id, CancellationToken ct)
	{
		var doc = await _col.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
		return doc is null ? null : ToDto(doc);
	}

	public async Task<BookDto> Create(CreateBookRequest req, CancellationToken ct)
	{
		var doc = new BookDocument
		{
			Title = req.Title.Trim(),
			Author = req.Author.Trim(),
			Year = req.Year,
			Status = string.IsNullOrWhiteSpace(req.Status) ? "Planned" : req.Status.Trim()
		};

		await _col.InsertOneAsync(doc, cancellationToken: ct);
		return ToDto(doc);
	}

	public async Task<bool> Update(string id, UpdateBookRequest req, CancellationToken ct)
	{
		var update = Builders<BookDocument>.Update
			.Set(x => x.Title, req.Title.Trim())
			.Set(x => x.Author, req.Author.Trim())
			.Set(x => x.Year, req.Year)
			.Set(x => x.Status, req.Status.Trim());

		var res = await _col.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
		return res.ModifiedCount == 1;
	}

	public async Task<bool> Delete(string id, CancellationToken ct)
	{
		var res = await _col.DeleteOneAsync(x => x.Id == id, ct);
		return res.DeletedCount == 1;
	}

	private static BookDto ToDto(BookDocument d) => new(d.Id, d.Title, d.Author, d.Year, d.Status);
}