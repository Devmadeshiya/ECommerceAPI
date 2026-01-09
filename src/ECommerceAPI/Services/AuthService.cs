using BCrypt.Net;
using ECommerceAPI.Data;
using ECommerceAPI.Models;
using ECommerceAPI.src.ECommerceAPI.Data;
using ECommerceAPI.src.ECommerceAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ECommerceAPI.Services;

public interface IAuthService
{
	Task<AuthResponse> RegisterAsync(RegisterRequest request);
	Task<AuthResponse> LoginAsync(LoginRequest request);
	Task<User?> GetUserByIdAsync(int userId);
}

public class AuthService : IAuthService
{
	private readonly ApplicationDbContext _context;
	private readonly IConfiguration _configuration;

	public AuthService(ApplicationDbContext context, IConfiguration configuration)
	{
		_context = context;
		_configuration = configuration;
	}

	public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
	{
		if (await _context.Users.AnyAsync(u => u.Email == request.Email))
		{
			return new AuthResponse
			{
				Success = false,
				Message = "Email already exists"
			};
		}

		var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

		var user = new User
		{
			Email = request.Email,
			PasswordHash = passwordHash,
			Role = request.Role,
			FullName = request.FullName,
			CreatedAt = DateTime.UtcNow
		};

		_context.Users.Add(user);

		if (request.Role.Equals("Seller", StringComparison.OrdinalIgnoreCase))
		{
			var sellerProfile = new SellerProfile
			{
				User = user
			};
			_context.SellerProfiles.Add(sellerProfile);
		}

		await _context.SaveChangesAsync();

		var token = GenerateJwtToken(user);

		return new AuthResponse
		{
			Success = true,
			Message = "Registration successful",
			Token = token,
			User = new UserDto
			{
				Id = user.Id,
				Email = user.Email,
				Role = user.Role,
				FullName = user.FullName
			}
		};
	}

	public async Task<AuthResponse> LoginAsync(LoginRequest request)
	{
		var user = await _context.Users
			.Include(u => u.SellerProfile)
			.FirstOrDefaultAsync(u => u.Email == request.Email);

		if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
		{
			return new AuthResponse
			{
				Success = false,
				Message = "Invalid email or password"
			};
		}

		if (!user.IsActive)
		{
			return new AuthResponse
			{
				Success = false,
				Message = "Account is inactive"
			};
		}

		var token = GenerateJwtToken(user);

		return new AuthResponse
		{
			Success = true,
			Message = "Login successful",
			Token = token,
			User = new UserDto
			{
				Id = user.Id,
				Email = user.Email,
				Role = user.Role,
				FullName = user.FullName
			}
		};
	}

	public async Task<User?> GetUserByIdAsync(int userId)
	{
		return await _context.Users
			.Include(u => u.SellerProfile)
			.FirstOrDefaultAsync(u => u.Id == userId);
	}

	private string GenerateJwtToken(User user)
	{
		var jwtSettings = _configuration.GetSection("JwtSettings");
		var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
		var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var claims = new[]
		{
			new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new Claim(ClaimTypes.Email, user.Email),
			new Claim(ClaimTypes.Role, user.Role),
			new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
		};

		var token = new JwtSecurityToken(
			issuer: jwtSettings["Issuer"],
			audience: jwtSettings["Audience"],
			claims: claims,
			expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryInMinutes"])),
			signingCredentials: credentials
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}