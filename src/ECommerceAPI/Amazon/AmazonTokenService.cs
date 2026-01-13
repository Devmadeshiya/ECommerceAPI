using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace ECommerceAPI.Amazon;

public class AmazonTokenService
{
	private readonly AmazonSettings _settings;
	private readonly HttpClient _http;

	public AmazonTokenService(
		IOptions<AmazonSettings> settings,
		HttpClient http)
	{
		_settings = settings.Value;
		_http = http;
	}

	// OAuth callback ke time
	public async Task<string> ExchangeCodeForRefreshToken(string code)
	{
		var content = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["grant_type"] = "authorization_code",
			["code"] = code,
			["client_id"] = _settings.ClientId,
			["client_secret"] = _settings.ClientSecret
		});

		var res = await _http.PostAsync(
			"https://api.amazon.com/auth/o2/token", content);

		res.EnsureSuccessStatusCode();

		var json = await res.Content.ReadAsStringAsync();
		dynamic data = JsonConvert.DeserializeObject(json);

		return data.refresh_token;
	}

	// Runtime pe har SP-API call ke liye
	public async Task<string> GetAccessToken(string refreshToken)
	{
		var content = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["grant_type"] = "refresh_token",
			["refresh_token"] = refreshToken,
			["client_id"] = _settings.ClientId,
			["client_secret"] = _settings.ClientSecret
		});

		var res = await _http.PostAsync(
			"https://api.amazon.com/auth/o2/token", content);

		res.EnsureSuccessStatusCode();

		var json = await res.Content.ReadAsStringAsync();
		dynamic data = JsonConvert.DeserializeObject(json);

		return data.access_token;
	}
}
