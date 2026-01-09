using ECommerceAPI.Models;
using ECommerceAPI.Services;
using ECommerceAPI.src.ECommerceAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerceAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Seller")]
public class SellerController : ControllerBase
{
	private readonly IAmazonSellerService _sellerService;

	public SellerController(IAmazonSellerService sellerService)
	{
		_sellerService = sellerService;
	}

	private int GetUserId()
	{
		var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		return int.Parse(userIdClaim ?? "0");
	}

	/// <summary>
	/// Get all products for the logged-in seller
	/// </summary>
	[HttpGet("products")]
	public async Task<IActionResult> GetProducts()
	{
		try
		{
			var sellerId = GetUserId();
			var products = await _sellerService.GetProductsAsync(sellerId);
			return Ok(new { success = true, data = products });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { success = false, message = ex.Message });
		}
	}

	/// <summary>
	/// Add a new product
	/// </summary>
	[HttpPost("products")]
	public async Task<IActionResult> AddProduct([FromBody] ProductDto product)
	{
		try
		{
			var sellerId = GetUserId();
			var result = await _sellerService.AddProductAsync(sellerId, product);
			return Ok(new { success = true, message = "Product added successfully", data = result });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { success = false, message = ex.Message });
		}
	}

	/// <summary>
	/// Update an existing product
	/// </summary>
	[HttpPut("products/{id}")]
	public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductDto product)
	{
		try
		{
			var sellerId = GetUserId();
			var result = await _sellerService.UpdateProductAsync(sellerId, id, product);
			return Ok(new { success = true, message = "Product updated successfully", data = result });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { success = false, message = ex.Message });
		}
	}

	/// <summary>
	/// Delete a product
	/// </summary>
	[HttpDelete("products/{id}")]
	public async Task<IActionResult> DeleteProduct(int id)
	{
		try
		{
			var sellerId = GetUserId();
			var result = await _sellerService.DeleteProductAsync(sellerId, id);

			if (!result)
				return NotFound(new { success = false, message = "Product not found" });

			return Ok(new { success = true, message = "Product deleted successfully" });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { success = false, message = ex.Message });
		}
	}

	/// <summary>
	/// Get all orders for the seller
	/// </summary>
	[HttpGet("orders")]
	public async Task<IActionResult> GetOrders()
	{
		try
		{
			var sellerId = GetUserId();
			var orders = await _sellerService.GetOrdersAsync(sellerId);
			return Ok(new { success = true, data = orders });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { success = false, message = ex.Message });
		}
	}

	/// <summary>
	/// Get order details by Amazon Order ID
	/// </summary>
	[HttpGet("orders/{amazonOrderId}")]
	public async Task<IActionResult> GetOrderDetails(string amazonOrderId)
	{
		try
		{
			var sellerId = GetUserId();
			var order = await _sellerService.GetOrderDetailsAsync(sellerId, amazonOrderId);

			if (order == null)
				return NotFound(new { success = false, message = "Order not found" });

			return Ok(new { success = true, data = order });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { success = false, message = ex.Message });
		}
	}
}