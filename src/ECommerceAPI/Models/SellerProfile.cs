using ECommerceAPI.Models;
using System.ComponentModel.DataAnnotations;

public class SellerProfile
{
	[Key]
	public int Id { get; set; }

	public int UserId { get; set; }
	public User User { get; set; } = null!;

	public string? AmazonSellerId { get; set; }
	public string? RefreshToken { get; set; }
	public string? MarketplaceId { get; set; }
	public string? StoreName { get; set; }

	public DateTime? ConnectedAt { get; set; }
}