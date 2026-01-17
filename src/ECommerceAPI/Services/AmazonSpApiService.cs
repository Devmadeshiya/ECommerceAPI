using ECommerceAPI.Data;
using ECommerceAPI.src.ECommerceAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ECommerceAPI.Amazon;

public class AmazonSpApiService
{
	private readonly AmazonSettings _settings;
	private readonly AmazonTokenService _tokenService;
	private readonly ApplicationDbContext _db;
	private readonly HttpClient _http;
	private readonly AwsSigV4Signer _signer;
	private readonly ILogger<AmazonSpApiService> _logger;

	public AmazonSpApiService(
		IOptions<AmazonSettings> settings,
		AmazonTokenService tokenService,
		ApplicationDbContext db,
		HttpClient http,
		AwsSigV4Signer signer,
		ILogger<AmazonSpApiService> logger)
	{
		_settings = settings.Value;
		_tokenService = tokenService;
		_db = db;
		_http = http;
		_signer = signer;
		_logger = logger;
	}

	public async Task<string> GetOrdersAsync(int userId)
	{
		try
		{
			_logger.LogInformation($"[ORDERS] Fetching orders for UserId: {userId}");

			// Get seller profile
			var sellerProfile = await _db.SellerProfiles
				.FirstOrDefaultAsync(x => x.UserId == userId);

			if (sellerProfile == null)
			{
				_logger.LogError($"[ORDERS] Seller profile not found for UserId: {userId}");
				throw new Exception("Seller profile not found");
			}

			_logger.LogInformation($"[ORDERS] Found SellerProfile Id: {sellerProfile.Id}");
			_logger.LogInformation($"[ORDERS] IsAmazonConnected: {sellerProfile.IsAmazonConnected}");
			_logger.LogInformation($"[ORDERS] Has RefreshToken: {!string.IsNullOrEmpty(sellerProfile.AmazonRefreshToken)}");

			if (!sellerProfile.IsAmazonConnected)
				throw new Exception("Seller not connected with Amazon");

			if (string.IsNullOrEmpty(sellerProfile.AmazonRefreshToken))
				throw new Exception("Amazon refresh token missing");

			// Get access token
			_logger.LogInformation("[ORDERS] Getting LWA access token...");
			var accessToken = await _tokenService.GetAccessToken(sellerProfile.AmazonRefreshToken);

			if (string.IsNullOrEmpty(accessToken))
				throw new Exception("Failed to get LWA access token");

			_logger.LogInformation($"[ORDERS] Got access token: {accessToken.Substring(0, 20)}...");

			// Prepare request
			var endpoint = "/orders/v0/orders";
			var createdAfter = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-ddTHH:mm:ssZ");
			var url = $"{_settings.BaseUrl}{endpoint}?MarketplaceIds={_settings.MarketplaceId}&CreatedAfter={createdAfter}";

			_logger.LogInformation($"[ORDERS] Request URL: {url}");

			var request = new HttpRequestMessage(HttpMethod.Get, url);
			request.Headers.Add("x-amz-access-token", accessToken);

			// AWS SigV4 Sign
			_logger.LogInformation("[ORDERS] Signing request with AWS SigV4...");

			if (string.IsNullOrEmpty(_settings.AwsAccessKey) || string.IsNullOrEmpty(_settings.AwsSecretKey))
			{
				throw new Exception("AWS credentials (AwsAccessKey/AwsSecretKey) are missing in configuration");
			}

			_signer.Sign(
				request,
				_settings.AwsAccessKey,
				_settings.AwsSecretKey,
				_settings.Region
			);

			// Send request
			_logger.LogInformation("[ORDERS] Sending request to Amazon SP-API...");
			var response = await _http.SendAsync(request);
			var body = await response.Content.ReadAsStringAsync();

			_logger.LogInformation($"[ORDERS] Response Status: {response.StatusCode}");
			_logger.LogInformation($"[ORDERS] Response Body: {body.Substring(0, Math.Min(500, body.Length))}...");

			if (!response.IsSuccessStatusCode)
			{
				_logger.LogError($"[ORDERS] Amazon API Error: {body}");
				throw new Exception($"Amazon API Error ({response.StatusCode}): {body}");
			}

			return body;
		}
		catch (Exception ex)
		{
			_logger.LogError($"[ORDERS ERROR] {ex.Message}");
			_logger.LogError($"[ORDERS ERROR] Stack: {ex.StackTrace}");
			throw;
		}
	}
}