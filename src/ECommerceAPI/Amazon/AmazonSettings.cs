namespace ECommerceAPI.Amazon;

public class AmazonSettings
{
	public string ApplicationId { get; set; } = string.Empty;
	public string ClientId { get; set; } = string.Empty;
	public string ClientSecret { get; set; } = string.Empty;
	public string? AwsAccessKey { get; set; }
	public string? AwsSecretKey { get; set; }
	public string? RoleArn { get; set; }
	public string Region { get; set; } = "eu-west-1";
	public string MarketplaceId { get; set; } = "A21TJRUUN4KGV";
	public string BaseUrl { get; set; } = "https://sellingpartnerapi-eu.amazon.com";
}