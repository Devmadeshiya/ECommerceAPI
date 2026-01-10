namespace ECommerceAPI.Models;

public class SearchRequest
{
	public string Keyword { get; set; } = string.Empty;
	public string? Category { get; set; }
	public int Page { get; set; } = 1;
	public int PageSize { get; set; } = 10;
}
