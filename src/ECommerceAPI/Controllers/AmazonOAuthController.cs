using ECommerceAPI.Amazon;
using ECommerceAPI.Data;
using ECommerceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace ECommerceAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AmazonOAuthController : ControllerBase
	{
		private readonly ApplicationDbContext _context;
		private readonly AmazonOAuthService _oauthService;
		private readonly AmazonTokenService _tokenService;
		private readonly AmazonSettings _amazonSettings;

		public AmazonOAuthController(
			ApplicationDbContext context,
			AmazonOAuthService oauthService,
			AmazonTokenService tokenService,
			IOptions<AmazonSettings> amazonSettings)
		{
			_context = context;
			_oauthService = oauthService;
			_tokenService = tokenService;
			_amazonSettings = amazonSettings.Value;
		}

		private int GetUserId()
		{
			var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			return int.Parse(userIdClaim ?? "0");
		}

		// Step 1: Get Amazon authorization URL
		[HttpGet("connect")]
		[Authorize(Roles = "Seller")]
		public IActionResult GetAuthorizationUrl()
		{
			try
			{
				var userId = GetUserId();
				var state = Guid.NewGuid().ToString(); // Generate unique state for CSRF protection

				// Store state in session or cache (for production, use distributed cache)
				HttpContext.Session.SetString($"amazon_oauth_state_{userId}", state);

				var authUrl = _oauthService.GetAuthorizationUrl(state);

				return Ok(new
				{
					success = true,
					authorizationUrl = authUrl,
					message = "Redirect user to this URL to authorize Amazon integration",
					state = state
				});
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Amazon OAuth error: {ex.Message}");
				return StatusCode(500, new
				{
					success = false,
					message = $"Failed to generate authorization URL: {ex.Message}"
				});
			}
		}

		// Step 2: Handle OAuth callback
		[HttpGet("callback")]
		[AllowAnonymous] // Allow anonymous since Amazon redirects here
		public async Task<IActionResult> HandleCallback(
			[FromQuery] string code,
			[FromQuery] string state,
			[FromQuery] string? spapi_oauth_code,
			[FromQuery] string? selling_partner_id)
		{
			try
			{
				// Use spapi_oauth_code if provided (this is the actual authorization code)
				var authCode = spapi_oauth_code ?? code;

				if (string.IsNullOrEmpty(authCode))
				{
					return BadRequest(new
					{
						success = false,
						message = "Authorization code is missing"
					});
				}

				// Validate state (retrieve from session - in production use distributed cache)
				// For now, we'll skip state validation but log it
				Console.WriteLine($"OAuth Callback - State: {state}, Code: {authCode}, Seller ID: {selling_partner_id}");

				// Exchange authorization code for tokens
				var tokenResponse = await _tokenService.ExchangeAuthorizationCodeAsync(authCode);

				if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.RefreshToken))
				{
					return BadRequest(new
					{
						success = false,
						message = "Failed to exchange authorization code for tokens"
					});
				}

				// In a real app, you'd extract userId from the state parameter
				// For now, we'll return the tokens and seller can complete connection manually
				return Ok(new
				{
					success = true,
					message = "Authorization successful! Use the /save-connection endpoint to complete setup",
					refreshToken = tokenResponse.RefreshToken,
					sellerId = selling_partner_id,
					expiresIn = tokenResponse.ExpiresIn
				});
			}
			catch (Exception ex)
			{
				Console.WriteLine($"OAuth callback error: {ex.Message}");
				Console.WriteLine($"Stack trace: {ex.StackTrace}");

				return StatusCode(500, new
				{
					success = false,
					message = $"OAuth callback failed: {ex.Message}"
				});
			}
		}

		// Step 3: Save connection details
		[HttpPost("save-connection")]
		public async Task<IActionResult> SaveConnection([FromBody] SaveConnectionRequest request)
		{
			try
			{
				if (string.IsNullOrEmpty(request.RefreshToken))
				{
					return BadRequest(new
					{
						success = false,
						message = "Refresh token is required"
					});
				}

				var userId = GetUserId();

				var sellerProfile = await _context.SellerProfiles
					.FirstOrDefaultAsync(sp => sp.UserId == userId);

				if (sellerProfile == null)
				{
					return NotFound(new
					{
						success = false,
						message = "Seller profile not found"
					});
				}

				// Update seller profile with Amazon connection details
				sellerProfile.AmazonRefreshToken = request.RefreshToken;
				sellerProfile.AmazonSellerId = request.SellerId;
				sellerProfile.IsAmazonConnected = true;

				await _context.SaveChangesAsync();

				return Ok(new
				{
					success = true,
					message = "Amazon account connected successfully",
					data = new
					{
						isConnected = true,
						sellerId = sellerProfile.AmazonSellerId
					}
				});
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Save connection error: {ex.Message}");
				return StatusCode(500, new
				{
					success = false,
					message = $"Failed to save connection: {ex.Message}"
				});
			}
		}

		// Get connection status
		[HttpGet("status")]
		public async Task<IActionResult> GetConnectionStatus()
		{
			try
			{
				var userId = GetUserId();

				var sellerProfile = await _context.SellerProfiles
					.FirstOrDefaultAsync(sp => sp.UserId == userId);

				if (sellerProfile == null)
				{
					return NotFound(new
					{
						success = false,
						message = "Seller profile not found"
					});
				}

				var isConnected = sellerProfile.IsAmazonConnected &&
								 !string.IsNullOrEmpty(sellerProfile.AmazonRefreshToken);

				return Ok(new
				{
					success = true,
					data = new
					{
						isConnected = isConnected,
						sellerId = sellerProfile.AmazonSellerId,
						storeName = sellerProfile.StoreName,
						connectedAt = sellerProfile.CreatedAt
					}
				});
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Get status error: {ex.Message}");
				return StatusCode(500, new
				{
					success = false,
					message = ex.Message
				});
			}
		}

		// Disconnect Amazon account
		[HttpPost("disconnect")]
		public async Task<IActionResult> DisconnectAmazon()
		{
			try
			{
				var userId = GetUserId();

				var sellerProfile = await _context.SellerProfiles
					.FirstOrDefaultAsync(sp => sp.UserId == userId);

				if (sellerProfile == null)
				{
					return NotFound(new
					{
						success = false,
						message = "Seller profile not found"
					});
				}

				// Clear Amazon connection details
				sellerProfile.AmazonRefreshToken = null;
				sellerProfile.AmazonSellerId = null;
				sellerProfile.IsAmazonConnected = false;

				await _context.SaveChangesAsync();

				return Ok(new
				{
					success = true,
					message = "Amazon account disconnected successfully"
				});
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Disconnect error: {ex.Message}");
				return StatusCode(500, new
				{
					success = false,
					message = ex.Message
				});
			}
		}

		// Test API access with stored refresh token
		[HttpPost("test-connection")]
		public async Task<IActionResult> TestConnection()
		{
			try
			{
				var userId = GetUserId();

				var sellerProfile = await _context.SellerProfiles
					.FirstOrDefaultAsync(sp => sp.UserId == userId);

				if (sellerProfile == null || !sellerProfile.IsAmazonConnected)
				{
					return BadRequest(new
					{
						success = false,
						message = "Amazon account not connected"
					});
				}

				// Get fresh access token using refresh token
				var accessToken = await _tokenService.GetAccessTokenAsync(sellerProfile.AmazonRefreshToken);

				if (string.IsNullOrEmpty(accessToken))
				{
					return StatusCode(500, new
					{
						success = false,
						message = "Failed to obtain access token. Connection may be invalid."
					});
				}

				return Ok(new
				{
					success = true,
					message = "Connection is valid and working",
					hasValidToken = true
				});
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Test connection error: {ex.Message}");
				return StatusCode(500, new
				{
					success = false,
					message = $"Connection test failed: {ex.Message}"
				});
			}
		}
	}

	// Request models
	public class SaveConnectionRequest
	{
		public string RefreshToken { get; set; }
		public string? SellerId { get; set; }
	}
}