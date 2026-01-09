using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models;

public class RegisterRequest
{
	[Required]
	[EmailAddress]
	public string Email { get; set; } = string.Empty;

	[Required]
	[MinLength(6)]
	public string Password { get; set; } = string.Empty;

	[Required]
	public string Role { get; set; } = "Buyer"; // Seller or Buyer

	public string? FullName { get; set; }
}
