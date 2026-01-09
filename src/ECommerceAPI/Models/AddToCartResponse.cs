public class AddToCartResponse
{
	public bool Success { get; set; }
	public string Message { get; set; } = string.Empty;
	public string? AmazonCheckoutUrl { get; set; }
}