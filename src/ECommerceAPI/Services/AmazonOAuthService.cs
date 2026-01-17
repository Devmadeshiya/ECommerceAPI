using Microsoft.Extensions.Options;
using System.Web;

namespace ECommerceAPI.Amazon;

public class AmazonOAuthService
{
	private readonly AmazonSettings _settings;
	private readonly ILogger<AmazonOAuthService> _logger;

	public AmazonOAuthService(
		IOptions<AmazonSettings> options,
		ILogger<AmazonOAuthService> logger)
	{
		_logger = logger;
		_settings = options.Value;

		if (_settings == null)
		{
			_logger.LogError("[OAUTH] AmazonSettings is NULL!");
			throw new Exception("AmazonSettings not loaded from configuration");
		}

		_logger.LogInformation($"[OAUTH INIT] ApplicationId: {_settings.ApplicationId ?? "NULL"}");
		_logger.LogInformation($"[OAUTH INIT] ClientId: {_settings.ClientId ?? "NULL"}");
		_logger.LogInformation($"[OAUTH INIT] Region: {_settings.Region ?? "NULL"}");
	}

	public string GetAuthorizationUrl(int sellerId)
	{
		if (string.IsNullOrWhiteSpace(_settings.ApplicationId))
		{
			_logger.LogError($"[OAUTH] ApplicationId is NULL! Settings: AppId={_settings.ApplicationId}");
			throw new Exception("Amazon ApplicationId is missing in configuration");
		}

		var query = HttpUtility.ParseQueryString(string.Empty);
		query["application_id"] = _settings.ApplicationId;
		query["state"] = sellerId.ToString();

		var authUrl = $"https://sellercentral.amazon.in/apps/authorize/consent?{query}";

		_logger.LogInformation($"[OAUTH] Generated auth URL for SellerId {sellerId}: {authUrl}");

		return authUrl;
	}
}