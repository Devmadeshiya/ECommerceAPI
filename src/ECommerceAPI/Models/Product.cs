using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models;

public class Product
{
	[Key]
	public int Id { get; set; }

	[Required]
	public int SellerId { get; set; }

	[Required]
	[MaxLength(20)]
	public string ASIN { get; set; } = string.Empty;

	[Required]
	[MaxLength(500)]
	public string Title { get; set; } = string.Empty;

	[MaxLength(2000)]
	public string? Description { get; set; }

	[Required]
	public decimal Price { get; set; }

	[MaxLength(1000)]
	public string? ImageUrl { get; set; }

	[MaxLength(100)]
	public string? Category { get; set; }

	public int Quantity { get; set; } = 0;

	public bool IsActive { get; set; } = true;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}