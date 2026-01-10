using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models;

public class AuthResponse
{
	public bool Success { get; set; }
	public string Message { get; set; } = string.Empty;
	public string? Token { get; set; }
	public UserDto? User { get; set; }
}