using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models;

public class User
{
	[Key]
	public int Id { get; set; }

	[Required]
	[EmailAddress]
	public string Email { get; set; } = string.Empty;

	[Required]
	public string PasswordHash { get; set; } = string.Empty;

	[Required]
	public string Role { get; set; } = "Buyer"; // Seller or Buyer

	public string? FullName { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public bool IsActive { get; set; } = true;

	// Navigation properties
	public SellerProfile? SellerProfile { get; set; }
	public ICollection<Order> Orders { get; set; } = new List<Order>();
}
