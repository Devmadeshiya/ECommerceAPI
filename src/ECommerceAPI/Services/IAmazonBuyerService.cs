using ECommerceAPI.Data;
using ECommerceAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerceAPI.src.ECommerceAPI.Services
{
	public interface IAmazonBuyerService
	{
		Task<List<Product>> SearchProductsAsync(SearchRequest searchRequest);
		Task<Product?> GetProductDetailsAsync(string asin);
		object GenerateAddToCartUrl(AddToCartRequest request);
		Task<List<Order>> GetBuyerOrdersAsync(int buyerId);
		Task<bool> ProcessBuyerOrderAsync(int orderId); // Added method
	}

	public class AmazonBuyerService : IAmazonBuyerService
	{
		private readonly ApplicationDbContext _context;

		public AmazonBuyerService(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<List<Product>> SearchProductsAsync(SearchRequest searchRequest)
		{
			var query = _context.Products.Where(p => p.IsActive);

			// Search by keyword
			if (!string.IsNullOrWhiteSpace(searchRequest.Keyword))
			{
				var keyword = searchRequest.Keyword.ToLower();
				query = query.Where(p =>
					p.Title.ToLower().Contains(keyword) ||
					(p.Description != null && p.Description.ToLower().Contains(keyword)));
			}

			// Filter by category
			if (!string.IsNullOrWhiteSpace(searchRequest.Category))
			{
				query = query.Where(p => p.Category == searchRequest.Category);
			}

			// Pagination
			var products = await query
				.OrderByDescending(p => p.CreatedAt)
				.Skip((searchRequest.Page - 1) * searchRequest.PageSize)
				.Take(searchRequest.PageSize)
				.ToListAsync();

			return products;
		}

		public async Task<Product?> GetProductDetailsAsync(string asin)
		{
			return await _context.Products
				.FirstOrDefaultAsync(p => p.ASIN == asin && p.IsActive);
		}

		public object GenerateAddToCartUrl(AddToCartRequest request)
		{
			var cartUrl = $"https://www.amazon.in/gp/aws/cart/add.html?ASIN.1={request.ASIN}&Quantity.1={request.Quantity}";

			return new
			{
				success = true,
				addToCartUrl = cartUrl,
				asin = request.ASIN,
				quantity = request.Quantity,
				message = "Redirect user to this URL to add product to Amazon cart"
			};
		}

		public async Task<List<Order>> GetBuyerOrdersAsync(int buyerId)
		{
			return await _context.Orders
				.Include(o => o.OrderItems)
				.Where(o => o.BuyerId == buyerId)
				.OrderByDescending(o => o.OrderDate)
				.ToListAsync();
		}

		public async Task<bool> ProcessBuyerOrderAsync(int orderId) // New method implementation
		{
			// Implement order processing logic here
			throw new NotImplementedException();
		}
	}
}