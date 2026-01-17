using ECommerceAPI.Data;
using ECommerceAPI.Models;
using ECommerceAPI.src.ECommerceAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerceAPI.Amazon;

[ApiController]
[Route("api/amazon")]
[Authorize]
public class AmazonController : ControllerBase
{
	private readonly AmazonOAuthService _oauth;
	private readonly AmazonTokenService _tokenService;
	private readonly AmazonSpApiService _spApi;
	private readonly ApplicationDbContext _db;
	private readonly ILogger<AmazonController> _logger;

	public AmazonController(
		AmazonOAuthService oauth,
		AmazonTokenService tokenService,
		AmazonSpApiService spApi,
		ApplicationDbContext db,
		ILogger<AmazonController> logger)
	{
		_oauth = oauth;
		_tokenService = tokenService;
		_spApi = spApi;
		_db = db;
		_logger = logger;
	}

	// 1️⃣ Seller → Amazon Connect
	[HttpGet("connect")]
	public async Task<IActionResult> ConnectAmazon()
	{
		try
		{
			var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
			if (idClaim == null)
				return Unauthorized("User Id not found in token");

			int userId = int.Parse(idClaim.Value);

			_logger.LogInformation($"[CONNECT] UserId: {userId} requesting Amazon connection");

			// Check if seller profile exists
			var sellerProfile = await _db.SellerProfiles
				.FirstOrDefaultAsync(x => x.UserId == userId);

			if (sellerProfile == null)
				return BadRequest("Seller profile not found. Please create a seller profile first.");

			var url = _oauth.GetAuthorizationUrl(sellerProfile.Id);

			_logger.LogInformation($"[CONNECT] Generated URL for SellerId: {sellerProfile.Id}");

			return Ok(new { redirectUrl = url });
		}
		catch (Exception ex)
		{
			_logger.LogError($"[CONNECT ERROR] {ex.Message}");
			return StatusCode(500, new { error = ex.Message });
		}
	}

	// 2️⃣ Amazon Callback
	[HttpGet("callback")]
	[AllowAnonymous]
	public async Task<IActionResult> Callback(
		[FromQuery] string? code,
		[FromQuery] string? state,
		[FromQuery] string? selling_partner_id)
	{
		try
		{
			_logger.LogInformation($"[CALLBACK] Received - Code: {code?.Substring(0, 10)}..., State: {state}, SPID: {selling_partner_id}");

			if (string.IsNullOrEmpty(code))
				return BadRequest("Authorization code missing");

			if (string.IsNullOrEmpty(state) || !int.TryParse(state, out int sellerId))
				return BadRequest("Invalid state parameter");

			// Exchange code for refresh token
			var refreshToken = await _tokenService.ExchangeCodeForRefreshToken(code);

			if (string.IsNullOrEmpty(refreshToken))
				return BadRequest("Failed to obtain refresh token");

			// Find seller profile by ID (state parameter contains SellerProfile.Id)
			var sellerProfile = await _db.SellerProfiles
				.FirstOrDefaultAsync(x => x.Id == sellerId);

			if (sellerProfile == null)
				return BadRequest($"Seller profile not found for ID: {sellerId}");

			// Update seller profile
			sellerProfile.AmazonRefreshToken = refreshToken;
			sellerProfile.IsAmazonConnected = true;
			sellerProfile.AmazonSellerId = selling_partner_id; // Store SPID

			await _db.SaveChangesAsync();

			_logger.LogInformation($"[CALLBACK SUCCESS] SellerId: {sellerId} connected successfully");

			// Redirect to frontend success page
			return Redirect("https://localhost:7050/swagger/index.html?success=true");
		}
		catch (Exception ex)
		{
			_logger.LogError($"[CALLBACK ERROR] {ex.Message}");
			return StatusCode(500, new { error = ex.Message, details = ex.StackTrace });
		}
	}

	// 3️⃣ Fetch Orders
	[HttpGet("orders")]
	public async Task<IActionResult> GetOrders()
	{
		try
		{
			var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
			if (idClaim == null)
				return Unauthorized("User Id not found in token");

			int userId = int.Parse(idClaim.Value);

			_logger.LogInformation($"[ORDERS] UserId: {userId} requesting orders");

			var data = await _spApi.GetOrdersAsync(userId);

			return Ok(data);
		}
		catch (Exception ex)
		{
			_logger.LogError($"[ORDERS ERROR] {ex.Message}");
			return StatusCode(500, new { error = ex.Message });
		}
	}

	// 🆕 Check Connection Status
	[HttpGet("status")]
	public async Task<IActionResult> GetConnectionStatus()
	{
		try
		{
			var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
			if (idClaim == null)
				return Unauthorized("User Id not found in token");

			int userId = int.Parse(idClaim.Value);

			var sellerProfile = await _db.SellerProfiles
				.FirstOrDefaultAsync(x => x.UserId == userId);

			if (sellerProfile == null)
				return NotFound("Seller profile not found");

			return Ok(new
			{
				isConnected = sellerProfile.IsAmazonConnected,
				hasRefreshToken = !string.IsNullOrEmpty(sellerProfile.AmazonRefreshToken),
				amazonSellerId = sellerProfile.AmazonSellerId
			});
		}
		catch (Exception ex)
		{
			_logger.LogError($"[STATUS ERROR] {ex.Message}");
			return StatusCode(500, new { error = ex.Message });
		}
	}
}