using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models;

public class SellerProfile
{
	[Key]
	public int Id { get; set; }

	public int UserId { get; set; }
	public User User { get; set; } = null!;

	public string? StoreName { get; set; }
	public string? AmazonSellerId { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
