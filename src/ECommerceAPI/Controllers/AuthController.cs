using ECommerceAPI.Data;
using ECommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace ECommerceAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AuthController : ControllerBase
	{
		private readonly ApplicationDbContext _context;
		private readonly IConfiguration _configuration;

		public AuthController(ApplicationDbContext context, IConfiguration configuration)
		{
			_context = context;
			_configuration = configuration;
		}

		[HttpPost("register")]
		public async Task<IActionResult> Register([FromBody] RegisterRequest request)
		{
			try
			{
				// Validate input
				if (string.IsNullOrWhiteSpace(request.Email) ||
					string.IsNullOrWhiteSpace(request.Password) ||
					string.IsNullOrWhiteSpace(request.Role) ||
					string.IsNullOrWhiteSpace(request.FullName))
				{
					return BadRequest(new
					{
						success = false,
						message = "Email, password, role, and full name are required",
						token = (string)null,
						user = (object)null
					});
				}

				// Validate role
				if (request.Role != "Buyer" && request.Role != "Seller")
				{
					return BadRequest(new
					{
						success = false,
						message = "Role must be either 'Buyer' or 'Seller'",
						token = (string)null,
						user = (object)null
					});
				}

				// Check if user already exists
				var existingUser = await _context.Users
					.FirstOrDefaultAsync(u => u.Email == request.Email);

				if (existingUser != null)
				{
					return BadRequest(new
					{
						success = false,
						message = "User with this email already exists",
						token = (string)null,
						user = (object)null
					});
				}

				// Hash password using BCrypt
				string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

				// Create new user
				var user = new User
				{
					Email = request.Email,
					PasswordHash = hashedPassword,
					Role = request.Role,
					FullName = request.FullName,
					IsActive = true,
					CreatedAt = DateTime.UtcNow
				};

				_context.Users.Add(user);
				await _context.SaveChangesAsync();

				// If role is Seller, create SellerProfile
				if (request.Role.Equals("Seller", StringComparison.OrdinalIgnoreCase))
				{
					var sellerProfile = new SellerProfile
					{
						UserId = user.Id,
						StoreName = request.StoreName,
						Description = request.Description,
						IsAmazonConnected = false,
						CreatedAt = DateTime.UtcNow
					};

					_context.SellerProfiles.Add(sellerProfile);
					await _context.SaveChangesAsync();
				}

				// Generate JWT token
				var token = GenerateJwtToken(user);

				return Ok(new
				{
					success = true,
					message = "User registered successfully",
					token = token,
					user = new
					{
						id = user.Id,
						email = user.Email,
						role = user.Role,
						fullName = user.FullName
					}
				});
			}
			catch (Exception ex)
			{
				// Log the exception here
				Console.WriteLine($"Registration error: {ex.Message}");
				Console.WriteLine($"Stack trace: {ex.StackTrace}");
				if (ex.InnerException != null)
				{
					Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
				}

				return StatusCode(500, new
				{
					success = false,
					message = $"An error occurred during registration: {ex.Message}",
					token = (string)null,
					user = (object)null
				});
			}
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginRequest request)
		{
			try
			{
				// Validate input
				if (string.IsNullOrWhiteSpace(request.Email) ||
					string.IsNullOrWhiteSpace(request.Password))
				{
					return BadRequest(new
					{
						success = false,
						message = "Email and password are required",
						token = (string)null,
						user = (object)null
					});
				}

				// Find user by email
				var user = await _context.Users
					.FirstOrDefaultAsync(u => u.Email == request.Email);

				if (user == null)
				{
					return Unauthorized(new
					{
						success = false,
						message = "Invalid email or password",
						token = (string)null,
						user = (object)null
					});
				}

				// Verify password using BCrypt
				bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

				if (!isPasswordValid)
				{
					return Unauthorized(new
					{
						success = false,
						message = "Invalid email or password",
						token = (string)null,
						user = (object)null
					});
				}

				// Check if user is active
				if (!user.IsActive)
				{
					return Unauthorized(new
					{
						success = false,
						message = "Your account has been deactivated",
						token = (string)null,
						user = (object)null
					});
				}

				// Generate JWT token
				var token = GenerateJwtToken(user);

				return Ok(new
				{
					success = true,
					message = "Login successful",
					token = token,
					user = new
					{
						id = user.Id,
						email = user.Email,
						role = user.Role,
						fullName = user.FullName
					}
				});
			}
			catch (Exception ex)
			{
				// Log the exception here
				Console.WriteLine($"Login error: {ex.Message}");
				Console.WriteLine($"Stack trace: {ex.StackTrace}");

				return StatusCode(500, new
				{
					success = false,
					message = $"An error occurred during login: {ex.Message}",
					token = (string)null,
					user = (object)null
				});
			}
		}

		private string GenerateJwtToken(User user)
		{
			var jwtSettings = _configuration.GetSection("JwtSettings");
			var secretKey = jwtSettings["Secret"] ?? "ThisIsAStrongJwtSecretKeyThatIsAtLeast32CharactersLong123!";

			var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
			var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

			var claims = new[]
			{
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new Claim(ClaimTypes.Email, user.Email),
				new Claim(ClaimTypes.Role, user.Role),
				new Claim(ClaimTypes.Name, user.FullName)
			};

			var token = new JwtSecurityToken(
				issuer: jwtSettings["Issuer"] ?? "ECommerceAPI",
				audience: jwtSettings["Audience"] ?? "ECommerceClient",
				claims: claims,
				expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpiryInMinutes"] ?? "60")),
				signingCredentials: credentials
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}

		// Get current user profile
		[HttpGet("me")]
		[Microsoft.AspNetCore.Authorization.Authorize]
		public async Task<IActionResult> GetCurrentUser()
		{
			try
			{
				var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				if (string.IsNullOrEmpty(userIdClaim))
				{
					return Unauthorized(new { success = false, message = "Invalid token" });
				}

				var userId = int.Parse(userIdClaim);
				var user = await _context.Users
					.Include(u => u.SellerProfile)
					.FirstOrDefaultAsync(u => u.Id == userId);

				if (user == null)
				{
					return NotFound(new { success = false, message = "User not found" });
				}

				var response = new
				{
					id = user.Id,
					email = user.Email,
					role = user.Role,
					fullName = user.FullName,
					isActive = user.IsActive,
					createdAt = user.CreatedAt,
					sellerProfile = user.Role == "Seller" && user.SellerProfile != null ? new
					{
						id = user.SellerProfile.Id,
						storeName = user.SellerProfile.StoreName,
						description = user.SellerProfile.Description,
						isAmazonConnected = user.SellerProfile.IsAmazonConnected,
						amazonSellerId = user.SellerProfile.AmazonSellerId
					} : null
				};

				return Ok(new { success = true, data = response });
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Get current user error: {ex.Message}");
				return StatusCode(500, new { success = false, message = ex.Message });
			}
		}
	}

	// Request Models
	public class RegisterRequest
	{
		public string Email { get; set; }
		public string Password { get; set; }
		public string Role { get; set; }
		public string FullName { get; set; }
		public string? StoreName { get; set; }
		public string? Description { get; set; }
	}

	public class LoginRequest
	{
		public string Email { get; set; }
		public string Password { get; set; }
	}
}