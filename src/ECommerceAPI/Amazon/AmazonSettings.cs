namespace ECommerceAPI.Amazon
{
	public class AmazonSettings
	{
		public string ApplicationId { get; set; } = string.Empty;
		public string ClientId { get; set; } = string.Empty;
		public string ClientSecret { get; set; } = string.Empty;
		public string Region { get; set; } = string.Empty;
		public string MarketplaceId { get; set; } = string.Empty;
		public string BaseUrl { get; set; } = string.Empty;
		public string AuthorizationUrl { get; set; } = string.Empty;
		public string TokenUrl { get; set; } = string.Empty;
		public string RedirectUri { get; set; } = string.Empty;
		public string State { get; set; } = string.Empty;
		public string RefreshToken { get; set; } = string.Empty;
	}
}