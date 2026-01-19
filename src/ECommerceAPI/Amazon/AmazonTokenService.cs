using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace ECommerceAPI.Amazon
{
	public class AmazonTokenService
	{
		private readonly HttpClient _httpClient;
		private readonly AmazonSettings _settings;

		public AmazonTokenService(HttpClient httpClient, IOptions<AmazonSettings> settings)
		{
			_httpClient = httpClient;
			_settings = settings.Value;
		}

		/// <summary>
		/// Exchange authorization code for refresh token
		/// </summary>
		public async Task<TokenResponse?> ExchangeAuthorizationCodeAsync(string authorizationCode)
		{
			try
			{
				var tokenEndpoint = "https://api.amazon.com/auth/o2/token";

				var requestBody = new Dictionary<string, string>
				{
					{ "grant_type", "authorization_code" },
					{ "code", authorizationCode },
					{ "client_id", _settings.ClientId },
					{ "client_secret", _settings.ClientSecret }
				};

				var content = new FormUrlEncodedContent(requestBody);
				var response = await _httpClient.PostAsync(tokenEndpoint, content);

				if (!response.IsSuccessStatusCode)
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					Console.WriteLine($"[Amazon Token] Exchange failed: {response.StatusCode} - {errorContent}");
					return null;
				}

				var jsonResponse = await response.Content.ReadAsStringAsync();
				var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(jsonResponse, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				Console.WriteLine($"[Amazon Token] Exchange successful");
				return tokenResponse;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[Amazon Token] Exchange error: {ex.Message}");
				return null;
			}
		}

		/// <summary>
		/// Get access token using refresh token
		/// </summary>
		public async Task<string?> GetAccessTokenAsync(string refreshToken)
		{
			try
			{
				var tokenEndpoint = "https://api.amazon.com/auth/o2/token";

				var requestBody = new Dictionary<string, string>
				{
					{ "grant_type", "refresh_token" },
					{ "refresh_token", refreshToken },
					{ "client_id", _settings.ClientId },
					{ "client_secret", _settings.ClientSecret }
				};

				var content = new FormUrlEncodedContent(requestBody);
				var response = await _httpClient.PostAsync(tokenEndpoint, content);

				if (!response.IsSuccessStatusCode)
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					Console.WriteLine($"[Amazon Token] Get access token failed: {response.StatusCode} - {errorContent}");
					return null;
				}

				var jsonResponse = await response.Content.ReadAsStringAsync();
				var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(jsonResponse, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				Console.WriteLine($"[Amazon Token] Access token obtained successfully");
				return tokenResponse?.AccessToken;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[Amazon Token] Get access token error: {ex.Message}");
				return null;
			}
		}
	}

	// Token Response Model
	public class TokenResponse
	{
		public string? AccessToken { get; set; }
		public string? RefreshToken { get; set; }
		public string? TokenType { get; set; }
		public int ExpiresIn { get; set; }
	}
}