using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models;

public class RegisterRequest
{
	[Required, EmailAddress]
	public string Email { get; set; } = string.Empty;

	[Required]
	public string Password { get; set; } = string.Empty;

	[Required]
	public string Role { get; set; } = string.Empty; // Seller / Buyer

	public string? FullName { get; set; }
}
