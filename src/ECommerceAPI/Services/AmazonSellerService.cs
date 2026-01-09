using ECommerceAPI.Data;
using ECommerceAPI.Models;
using ECommerceAPI.src.ECommerceAPI.Data;
using ECommerceAPI.src.ECommerceAPI.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RestSharp;
using System.Security.Cryptography;
using System.Text;

namespace ECommerceAPI.Services;

public interface IAmazonSellerService
{
	Task<List<ProductDto>> GetProductsAsync(int sellerId);
	Task<ProductDto> AddProductAsync(int sellerId, ProductDto product);
	Task<ProductDto> UpdateProductAsync(int sellerId, int productId, ProductDto product);
	Task<bool> DeleteProductAsync(int sellerId, int productId);
	Task<List<OrderDto>> GetOrdersAsync(int sellerId);
	Task<OrderDto?> GetOrderDetailsAsync(int sellerId, string amazonOrderId);
}

public class AmazonSellerService : IAmazonSellerService
{
	private readonly ApplicationDbContext _context;
	private readonly IConfiguration _configuration;
	private readonly string _clientId;
	private readonly string _clientSecret;
	private readonly string _refreshToken;
	private readonly string _baseUrl;
	private readonly string _marketplaceId;
	private string? _accessToken;

	public AmazonSellerService(ApplicationDbContext context, IConfiguration configuration)
	{
		_context = context;
		_configuration = configuration;
		_clientId = _configuration["AmazonSpApi:ClientId"] ?? "";
		_clientSecret = _configuration["AmazonSpApi:ClientSecret"] ?? "";
		_refreshToken = _configuration["AmazonSpApi:RefreshToken"] ?? "";
		_baseUrl = _configuration["AmazonSpApi:BaseUrl"] ?? "";
		_marketplaceId = _configuration["AmazonSpApi:MarketplaceId"] ?? "";
	}

	public async Task<List<ProductDto>> GetProductsAsync(int sellerId)
	{
		var products = await _context.Products
			.Where(p => p.SellerId == sellerId && p.IsActive)
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

	public async Task<ProductDto> AddProductAsync(int sellerId, ProductDto productDto)
	{
		var product = new Product
		{
			ASIN = productDto.ASIN,
			Title = productDto.Title,
			Description = productDto.Description,
			Price = productDto.Price,
			ImageUrl = productDto.ImageUrl,
			Category = productDto.Category,
			Quantity = productDto.Quantity,
			SellerId = sellerId,
			CreatedAt = DateTime.UtcNow,
			IsActive = true
		};

		_context.Products.Add(product);
		await _context.SaveChangesAsync();

		productDto.Id = product.Id;
		return productDto;
	}

	public async Task<ProductDto> UpdateProductAsync(int sellerId, int productId, ProductDto productDto)
	{
		var product = await _context.Products
			.FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == sellerId);

		if (product == null)
			throw new Exception("Product not found");

		product.Title = productDto.Title;
		product.Description = productDto.Description;
		product.Price = productDto.Price;
		product.ImageUrl = productDto.ImageUrl;
		product.Category = productDto.Category;
		product.Quantity = productDto.Quantity;
		product.UpdatedAt = DateTime.UtcNow;

		await _context.SaveChangesAsync();

		productDto.Id = product.Id;
		return productDto;
	}

	public async Task<bool> DeleteProductAsync(int sellerId, int productId)
	{
		var product = await _context.Products
			.FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == sellerId);

		if (product == null)
			return false;

		product.IsActive = false;
		await _context.SaveChangesAsync();

		return true;
	}

	public async Task<List<OrderDto>> GetOrdersAsync(int sellerId)
	{
		var orders = await _context.Orders
			.Include(o => o.OrderItems)
			.Where(o => o.OrderItems.Any(oi =>
				_context.Products.Any(p => p.ASIN == oi.ASIN && p.SellerId == sellerId)))
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

	public async Task<OrderDto?> GetOrderDetailsAsync(int sellerId, string amazonOrderId)
	{
		var order = await _context.Orders
			.Include(o => o.OrderItems)
			.Where(o => o.AmazonOrderId == amazonOrderId)
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
			.FirstOrDefaultAsync();

		return order;
	}

	private async Task<string> GetAccessTokenAsync()
	{
		if (!string.IsNullOrEmpty(_accessToken))
			return _accessToken;

		var client = new RestClient("https://api.amazon.com");
		var request = new RestRequest("/auth/o2/token", Method.Post);

		request.AddParameter("grant_type", "refresh_token");
		request.AddParameter("refresh_token", _refreshToken);
		request.AddParameter("client_id", _clientId);
		request.AddParameter("client_secret", _clientSecret);

		var response = await client.ExecuteAsync(request);

		if (response.IsSuccessful && response.Content != null)
		{
			var tokenResponse = JsonConvert.DeserializeObject<dynamic>(response.Content);
			_accessToken = tokenResponse?.access_token;
			return _accessToken ?? "";
		}

		throw new Exception("Failed to obtain access token");
	}
}