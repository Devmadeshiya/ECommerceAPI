namespace ECommerceAPI.Models;

public class ProductDto
{
	public int? Id { get; set; }
	public string ASIN { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public string? Description { get; set; }
	public decimal Price { get; set; }
	public string? ImageUrl { get; set; }
	public string? Category { get; set; }
	public int Quantity { get; set; }
}
