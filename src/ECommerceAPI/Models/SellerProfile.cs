
namespace ECommerceAPI.Models;

public class SellerProfile
{
	public int Id { get; set; }
	public int UserId { get; set; }

	// Basic Info
	public string? StoreName { get; set; }
	public string? Description { get; set; }

	// Amazon Integration (should appear ONLY ONCE)
	public string? AmazonRefreshToken { get; set; }
	public bool IsAmazonConnected { get; set; }
	public string? AmazonSellerId { get; set; }

	// Navigation
	public User? User { get; set; }
	public DateTime CreatedAt { get; internal set; }
}