using ECommerceAPI.Data;
using ECommerceAPI.Models;
using Microsoft.EntityFrameworkCore;
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
	private readonly string _partnerTag;
	private readonly string _marketplaceUrl;

	public AmazonBuyerService(ApplicationDbContext context, IConfiguration configuration)
	{
		_context = context;
		_configuration = configuration;
		_partnerTag = _configuration["AmazonPAAPI:PartnerTag"] ?? "";
		_marketplaceUrl = _configuration["AmazonPAAPI:MarketplaceUrl"] ?? "";
	}

	// ===================== SEARCH PRODUCTS =====================

	public async Task<List<ProductDto>> SearchProductsAsync(SearchRequest searchRequest)
	{
		try
		{
			return await _context.Products
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
		}
		catch
		{
			return new List<ProductDto>();
		}
	}

	// ===================== PRODUCT DETAILS =====================

	public async Task<ProductDto?> GetProductDetailsAsync(string asin)
	{
		return await _context.Products
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
	}

	// ===================== ADD TO CART =====================

	public AddToCartResponse GenerateAddToCartUrl(AddToCartRequest request)
	{
		try
		{
			var baseUrl = $"{_marketplaceUrl}/gp/aws/cart/add.html";

			var parameters = new Dictionary<string, string>
			{
				{ "AssociateTag", _partnerTag },
				{ "ASIN.1", request.ASIN },
				{ "Quantity.1", request.Quantity.ToString() }
			};

			var queryString = string.Join("&", parameters.Select(p =>
				$"{HttpUtility.UrlEncode(p.Key)}={HttpUtility.UrlEncode(p.Value)}"));

			return new AddToCartResponse
			{
				Success = true,
				Message = "Redirect to Amazon to complete purchase",
				AmazonCheckoutUrl = $"{baseUrl}?{queryString}"
			};
		}
		catch (Exception ex)
		{
			return new AddToCartResponse
			{
				Success = false,
				Message = ex.Message
			};
		}
	}

	// ===================== BUYER ORDERS =====================

	public async Task<List<OrderDto>> GetBuyerOrdersAsync(int buyerId)
	{
		return await _context.Orders
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
	}
}
