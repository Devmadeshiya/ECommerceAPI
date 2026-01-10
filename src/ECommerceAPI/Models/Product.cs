using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models;

public class Product
{
	[Key]
	public int Id { get; set; }

	[Required]
	public int SellerId { get; set; }

	[Required]
	public string ASIN { get; set; } = string.Empty;

	[Required]
	public string Title { get; set; } = string.Empty;

	public string? Description { get; set; }

	public decimal Price { get; set; }

	public string? ImageUrl { get; set; }

	public string? Category { get; set; }

	public int Quantity { get; set; }

	public bool IsActive { get; set; } = true;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime UpdatedAt { get; internal set; }
	public object Seller { get; internal set; }
}
