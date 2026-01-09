using ECommerceAPI.Data;
using ECommerceAPI.Models;
using ECommerceAPI.src.ECommerceAPI.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RestSharp;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace ECommerceAPI.Services;

public interface IAmazonBuyerService
{
	Task<List<ProductDto>> SearchProductsAsync(SearchRequest request);
	Task<ProductDto?> GetProductDetailsAsync(string asin);
	AddToCartResponse GenerateAddToCartUrl(AddToCartRequest request);
	Task<List<OrderDto>> GetBuyerOrdersAsync(int buyerId);
}

public class AmazonBuyerService : IAmazonBuyerService
{
	private readonly ApplicationDbContext _context;
	private readonly IConfiguration _configuration;
	private readonly string _accessKey;
	private readonly string _secretKey;
	private readonly string _partnerTag;
	private readonly string _host;
	private readonly string _region;
	private readonly string _marketplaceUrl;

	public AmazonBuyerService(ApplicationDbContext context, IConfiguration configuration)
	{
		_context = context;
		_configuration = configuration;
		_accessKey = _configuration["AmazonPAAPI:AccessKey"] ?? "";
		_secretKey = _configuration["AmazonPAAPI:SecretKey"] ?? "";
		_partnerTag = _configuration["AmazonPAAPI:PartnerTag"] ?? "";
		_host = _configuration["AmazonPAAPI:Host"] ?? "";
		_region = _configuration["AmazonPAAPI:Region"] ?? "";
		_marketplaceUrl = _configuration["AmazonPAAPI:MarketplaceUrl"] ?? "";
	}

	public async Task<List<ProductDto>> SearchProductsAsync(SearchRequest searchRequest)
	{
		try
		{
			var products = await _context.Products
				.Where(p => p.IsActive &&
					(p.Title.Contains(searchRequest.Keyword) ||
					 p.Description!.Contains(searchRequest.Keyword)))
				.Skip((searchRequest.Page - 1) * searchRequest.PageSize)
				.Take(searchRequest.PageSize)
				.Select(p => new ProductDto
				{
					Id = p.Id,
					ASIN = p.ASIN,
					Title = p.Title,
					Description = p.Description,
					Price = p.Price,
					ImageUrl = p.ImageUrl,
					Category = p.Category,
					Quantity = p.Quantity
				})
				.ToListAsync();

			return products;
		}
		catch
		{
			return new List<ProductDto>();
		}
	}

	public async Task<ProductDto?> GetProductDetailsAsync(string asin)
	{
		var product = await _context.Products
			.Where(p => p.ASIN == asin && p.IsActive)
			.Select(p => new ProductDto
			{
				Id = p.Id,
				ASIN = p.ASIN,
				Title = p.Title,
				Description = p.Description,
				Price = p.Price,
				ImageUrl = p.ImageUrl,
				Category = p.Category,
				Quantity = p.Quantity
			})
			.FirstOrDefaultAsync();

		return product;
	}

	public AddToCartResponse GenerateAddToCartUrl(AddToCartRequest request)
	{
		try
		{
			var baseUrl = $"{_marketplaceUrl}/gp/aws/cart/add.html";

			var parameters = new Dictionary<string, string>
			{
				{ "AssociateTag", _partnerTag },
				{ $"ASIN.1", request.ASIN },
				{ $"Quantity.1", request.Quantity.ToString() }
			};

			var queryString = string.Join("&", parameters.Select(p =>
				$"{HttpUtility.UrlEncode(p.Key)}={HttpUtility.UrlEncode(p.Value)}"));

			var checkoutUrl = $"{baseUrl}?{queryString}";

			return new AddToCartResponse
			{
				Success = true,
				Message = "Redirect to Amazon to complete purchase",
				AmazonCheckoutUrl = checkoutUrl
			};
		}
		catch (Exception ex)
		{
			return new AddToCartResponse
			{
				Success = false,
				Message = $"Error generating checkout URL: {ex.Message}"
			};
		}
	}

	public async Task<List<OrderDto>> GetBuyerOrdersAsync(int buyerId)
	{
		var orders = await _context.Orders
			.Include(o => o.OrderItems)
			.Where(o => o.BuyerId == buyerId)
			.OrderByDescending(o => o.OrderDate)
			.Select(o => new OrderDto
			{
				Id = o.Id,
				AmazonOrderId = o.AmazonOrderId,
				Status = o.Status,
				TotalAmount = o.TotalAmount,
				OrderDate = o.OrderDate,
				Items = o.OrderItems.Select(oi => new OrderItemDto
				{
					ASIN = oi.ASIN,
					ProductTitle = oi.ProductTitle,
					UnitPrice = oi.UnitPrice,
					Quantity = oi.Quantity,
					TotalPrice = oi.TotalPrice
				}).ToList()
			})
			.ToListAsync();

		return orders;
	}

	private string CreatePAAPISignature(string stringToSign, string secretKey)
	{
		using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
		var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
		return Convert.ToBase64String(hashBytes);
	}

	private Dictionary<string, string> GetPAAPIHeaders(string path, string payload)
	{
		var timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
		var date = DateTime.UtcNow.ToString("yyyyMMdd");

		var canonicalRequest = $"POST\n{path}\n\nhost:{_host}\nx-amz-date:{timestamp}\n\nhost;x-amz-date\n{GetSHA256Hash(payload)}";

		var credentialScope = $"{date}/{_region}/ProductAdvertisingAPI/aws4_request";
		var stringToSign = $"AWS4-HMAC-SHA256\n{timestamp}\n{credentialScope}\n{GetSHA256Hash(canonicalRequest)}";

		var signature = CreatePAAPISignature(stringToSign, _secretKey);

		return new Dictionary<string, string>
		{
			{ "Authorization", $"AWS4-HMAC-SHA256 Credential={_accessKey}/{credentialScope}, SignedHeaders=host;x-amz-date, Signature={signature}" },
			{ "x-amz-date", timestamp },
			{ "host", _host }
		};
	}

	private string GetSHA256Hash(string input)
	{
		using var sha256 = SHA256.Create();
		var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
		return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
	}
}