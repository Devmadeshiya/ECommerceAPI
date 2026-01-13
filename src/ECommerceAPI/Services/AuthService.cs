using BCrypt.Net;
using ECommerceAPI.Data;
using ECommerceAPI.Models;
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

	// ========================= REGISTER =========================

	public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
	{
		try
		{
			// 1️⃣ Duplicate email check
			if (await _context.Users.AnyAsync(x => x.Email == request.Email))
			{
				return new AuthResponse
				{
					Success = false,
					Message = "Email already exists"
				};
			}

			// 2️⃣ Create user
			var user = new User
			{
				Email = request.Email,
				PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
				Role = request.Role,
				FullName = request.FullName,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			_context.Users.Add(user);
			await _context.SaveChangesAsync(); // 🔥 User must be saved first

			// 3️⃣ Create SellerProfile ONLY if role = Seller AND not exists
			if (request.Role.Equals("Seller", StringComparison.OrdinalIgnoreCase))
			{
				var existingSellerProfile = await _context.SellerProfiles
					.FirstOrDefaultAsync(x => x.UserId == user.Id);

				if (existingSellerProfile == null)
				{
					var sellerProfile = new SellerProfile
					{
						UserId = user.Id,
						CreatedAt = DateTime.UtcNow,
						IsAmazonConnected = false
					};

					_context.SellerProfiles.Add(sellerProfile);
					await _context.SaveChangesAsync();
				}
			}

			// 4️⃣ Generate JWT
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
		catch (Exception ex)
		{
			Console.WriteLine("REGISTER ERROR: " + ex.Message);
			if (ex.InnerException != null)
			{
				Console.WriteLine("INNER: " + ex.InnerException.Message);
			}

			return new AuthResponse
			{
				Success = false,
				Message = "Registration failed due to server error"
			};
		}
	}

	// ========================= LOGIN =========================

	public async Task<AuthResponse> LoginAsync(LoginRequest request)
	{
		try
		{
			var user = await _context.Users
				.Include(x => x.SellerProfile)
				.FirstOrDefaultAsync(x => x.Email == request.Email);

			if (user == null)
			{
				return new AuthResponse
				{
					Success = false,
					Message = "Invalid email or password"
				};
			}

			if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
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
		catch (Exception ex)
		{
			Console.WriteLine("LOGIN ERROR: " + ex.Message);
			if (ex.InnerException != null)
			{
				Console.WriteLine("INNER: " + ex.InnerException.Message);
			}

			return new AuthResponse
			{
				Success = false,
				Message = "Login failed due to server error"
			};
		}
	}

	// ========================= GET USER =========================

	public async Task<User?> GetUserByIdAsync(int userId)
	{
		return await _context.Users
			.Include(x => x.SellerProfile)
			.FirstOrDefaultAsync(x => x.Id == userId);
	}

	// ========================= JWT =========================

	private string GenerateJwtToken(User user)
	{
		var jwtSettings = _configuration.GetSection("JwtSettings");

		var secret = jwtSettings["Secret"];
		if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
		{
			throw new Exception("JWT Secret not configured properly");
		}

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
		var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var claims = new List<Claim>
		{
			new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new Claim(ClaimTypes.Email, user.Email),
			new Claim(ClaimTypes.Role, user.Role),
			new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
		};

		var issuer = jwtSettings["Issuer"];
		var audience = jwtSettings["Audience"];
		var expiryMinutes = Convert.ToDouble(jwtSettings["ExpiryInMinutes"] ?? "60");

		var token = new JwtSecurityToken(
			issuer: issuer,
			audience: audience,
			claims: claims,
			expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
			signingCredentials: credentials
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}
