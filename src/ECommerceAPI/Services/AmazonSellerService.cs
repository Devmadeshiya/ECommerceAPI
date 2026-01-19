using ECommerceAPI.Data;
using ECommerceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Services
{
	public class AmazonSellerService : IAmazonSellerService
	{
		private readonly ApplicationDbContext _context;

		public AmazonSellerService(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<List<Product>> GetProductsAsync(int userId)
		{
			// Get seller profile ID from user ID
			var sellerProfile = await _context.SellerProfiles
				.FirstOrDefaultAsync(sp => sp.UserId == userId);

			if (sellerProfile == null)
				return new List<Product>();

			return await _context.Products
				.Where(p => p.SellerId == sellerProfile.Id)
				.OrderByDescending(p => p.CreatedAt)
				.ToListAsync();
		}

		public async Task<Product> AddProductAsync(int userId, ProductDto productDto)
		{
			var sellerProfile = await _context.SellerProfiles
				.FirstOrDefaultAsync(sp => sp.UserId == userId);

			if (sellerProfile == null)
				throw new InvalidOperationException("Seller profile not found");

			var product = new Product
			{
				SellerId = sellerProfile.Id,
				ASIN = productDto.ASIN,
				Title = productDto.Title,
				Description = productDto.Description,
				Price = productDto.Price,
				Quantity = productDto.Quantity,
				ImageUrl = productDto.ImageUrl,
				Category = productDto.Category,
				IsActive = true,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			_context.Products.Add(product);
			await _context.SaveChangesAsync();

			return product;
		}

		public async Task<Product> UpdateProductAsync(int userId, int productId, ProductDto productDto)
		{
			var sellerProfile = await _context.SellerProfiles
				.FirstOrDefaultAsync(sp => sp.UserId == userId);

			if (sellerProfile == null)
				throw new InvalidOperationException("Seller profile not found");

			var product = await _context.Products
				.FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == sellerProfile.Id);

			if (product == null)
				throw new InvalidOperationException("Product not found");

			product.ASIN = productDto.ASIN;
			product.Title = productDto.Title;
			product.Description = productDto.Description;
			product.Price = productDto.Price;
			product.Quantity = productDto.Quantity;
			product.ImageUrl = productDto.ImageUrl;
			product.Category = productDto.Category;
			product.UpdatedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			return product;
		}

		public async Task<bool> DeleteProductAsync(int userId, int productId)
		{
			var sellerProfile = await _context.SellerProfiles
				.FirstOrDefaultAsync(sp => sp.UserId == userId);

			if (sellerProfile == null)
				return false;

			var product = await _context.Products
				.FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == sellerProfile.Id);

			if (product == null)
				return false;

			_context.Products.Remove(product);
			await _context.SaveChangesAsync();

			return true;
		}

		public async Task<List<Order>> GetOrdersAsync(int userId)
		{
			var sellerProfile = await _context.SellerProfiles
				.FirstOrDefaultAsync(sp => sp.UserId == userId);

			if (sellerProfile == null)
				return new List<Order>();

			// Get orders containing products from this seller
			return await _context.Orders
				.Include(o => o.OrderItems)
				.Include(o => o.Buyer)
				.Where(o => o.OrderItems.Any(oi =>
					_context.Products.Any(p => p.ASIN == oi.ASIN && p.SellerId == sellerProfile.Id)))
				.OrderByDescending(o => o.OrderDate)
				.ToListAsync();
		}
	}
}