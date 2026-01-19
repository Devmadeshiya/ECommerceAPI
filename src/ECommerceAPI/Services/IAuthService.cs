using ECommerceAPI.Models;

namespace ECommerceAPI.Services
{
	public interface IAuthService
	{
		Task<(bool Success, string Message, string? Token, object? User)> RegisterAsync(string email, string password, string role, string fullName, string? storeName = null, string? description = null);
		Task<(bool Success, string Message, string? Token, object? User)> LoginAsync(string email, string password);
		Task<User?> GetUserByIdAsync(int userId);
	}
}