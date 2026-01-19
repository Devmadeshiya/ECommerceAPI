using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceAPI.Models;

public class SellerProfile
{
	[Key]
	public int Id { get; set; }

	[Required]
	public int UserId { get; set; }

	// Basic Info
	public string? StoreName { get; set; }
	public string? Description { get; set; }

	// Amazon Integration
	public string? AmazonRefreshToken { get; set; }
	public bool IsAmazonConnected { get; set; } = false;
	public string? AmazonSellerId { get; set; }

	// Timestamps
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	// Navigation
	[ForeignKey("UserId")]
	public User User { get; set; } = null!;
	public string BusinessName { get; internal set; }
}