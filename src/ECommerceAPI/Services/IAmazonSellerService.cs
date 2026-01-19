using ECommerceAPI.Models;

namespace ECommerceAPI.Services
{
	public interface IAmazonSellerService
	{
		Task<List<Product>> GetProductsAsync(int sellerId);
		Task<Product> AddProductAsync(int sellerId, ProductDto productDto);
		Task<Product> UpdateProductAsync(int sellerId, int productId, ProductDto productDto);
		Task<bool> DeleteProductAsync(int sellerId, int productId);
		Task<List<Order>> GetOrdersAsync(int sellerId);
	}
}