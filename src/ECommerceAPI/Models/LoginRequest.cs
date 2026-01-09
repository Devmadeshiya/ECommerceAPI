using System.ComponentModel.DataAnnotations;

public class LoginRequest
{
	[Required]
	[EmailAddress]
	public string Email { get; set; } = string.Empty;

	[Required]
	public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
	public bool Success { get; set; }
	public string Message { get; set; } = string.Empty;
	public string? Token { get; set; }
	public UserDto? User { get; set; }
}