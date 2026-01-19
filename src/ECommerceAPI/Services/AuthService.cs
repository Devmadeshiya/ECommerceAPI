using ECommerceAPI.Data;
using ECommerceAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ECommerceAPI.Services
{
	public class AuthService : IAuthService
	{
		private readonly ApplicationDbContext _context;
		private readonly IConfiguration _configuration;

		public AuthService(ApplicationDbContext context, IConfiguration configuration)
		{
			_context = context;
			_configuration = configuration;
		}

		public async Task<(bool Success, string Message, string? Token, object? User)> RegisterAsync(string email, string password, string role, string fullName, string? storeName = null, string? description = null)
		{
			if (await _context.Users.AnyAsync(u => u.Email == email))
			{
				return (false, "Email already exists.", null, null);
			}

			var user = new User
			{
				Email = email,
				PasswordHash = HashPassword(password),
				Role = role,
				CreatedAt = DateTime.UtcNow,
				IsActive = true
			};

			_context.Users.Add(user);
			await _context.SaveChangesAsync();

			if (role == "Seller")
			{
				var sellerProfile = new SellerProfile
				{
					UserId = user.UserId,
					BusinessName = storeName ?? email.Split('@')[0],
					Description = description,
					IsAmazonConnected = false
				};
				_context.SellerProfiles.Add(sellerProfile);
				await _context.SaveChangesAsync();
			}

			var token = GenerateJwtToken(user);

			return (true, "Registration successful.", token, new
			{
				user.UserId,
				user.Email,
				user.Role
			});
		}

		public async Task<(bool Success, string Message, string? Token, object? User)> LoginAsync(string email, string password)
		{
			var user = await _context.Users
				.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

			if (user == null || !VerifyPassword(password, user.PasswordHash))
			{
				return (false, "Invalid credentials.", null, null);
			}

			var token = GenerateJwtToken(user);

			return (true, "Login successful.", token, new
			{
				user.UserId,
				user.Email,
				user.Role
			});
		}

		public async Task<User?> GetUserByIdAsync(int userId)
		{
			return await _context.Users
				.Include(u => u.SellerProfile)
				.FirstOrDefaultAsync(u => u.UserId == userId);
		}

		private string HashPassword(string password)
		{
			using var sha256 = SHA256.Create();
			var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
			return Convert.ToBase64String(hashedBytes);
		}

		private bool VerifyPassword(string password, string hash)
		{
			return HashPassword(password) == hash;
		}

		private string GenerateJwtToken(User user)
		{
			var jwtSettings = _configuration.GetSection("JwtSettings");
			var secretKey = jwtSettings["Secret"];

			// Use default if not configured
			if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
			{
				secretKey = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
			}

			var issuer = jwtSettings["Issuer"] ?? "ECommerceAPI";
			var audience = jwtSettings["Audience"] ?? "ECommerceAPIUsers";

			var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
			var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

			var claims = new[]
			{
				new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
				new Claim(ClaimTypes.Email, user.Email),
				new Claim(ClaimTypes.Role, user.Role),
				new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
				new Claim(JwtRegisteredClaimNames.Email, user.Email),
				new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
			};

			var token = new JwtSecurityToken(
				issuer: issuer,
				audience: audience,
				claims: claims,
				notBefore: DateTime.UtcNow,
				expires: DateTime.UtcNow.AddHours(24),
				signingCredentials: credentials
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}
	}
}