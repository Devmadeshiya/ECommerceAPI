using ECommerceAPI.Models;
using ECommerceAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerceAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Buyer")]
public class BuyerController : ControllerBase
{
	private readonly IAmazonBuyerService _buyerService;

	public BuyerController(IAmazonBuyerService buyerService)
	{
		_buyerService = buyerService;
	}

	private int GetUserId()
	{
		var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		return int.Parse(userIdClaim ?? "0");
	}

	// ===================== SEARCH PRODUCTS =====================

	[HttpGet("search")]
	public async Task<IActionResult> SearchProducts(
		[FromQuery] string keyword,
		[FromQuery] string? category = null,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 10)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(keyword))
				return BadRequest(new { success = false, message = "Keyword is required" });

			var searchRequest = new SearchRequest
			{
				Keyword = keyword,
				Category = category,
				Page = page,
				PageSize = pageSize
			};

			await _buyerService.SearchProductsAsync(searchRequest);
			var products = new List<object>(); // Replace 'object' with the actual product type returned by your service

			return Ok(new
			{
				success = true,
				data = products,
				page,
				pageSize,
				total = products.Count
			});
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { success = false, message = ex.Message });
		}
	}

	// ===================== PRODUCT DETAILS =====================

	[HttpGet("product/{asin}")]
	public async Task<IActionResult> GetProductDetails(string asin)
	{
		try
		{
			// The IAmazonBuyerService interface does not define GetProductDetailsAsync.
			// You need to implement this method in your service and interface.
			// For now, you can return a NotImplemented result or ask for the correct method.
			return StatusCode(501, new { success = false, message = "GetProductDetailsAsync is not implemented in the service." });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { success = false, message = ex.Message });
		}
	}

	// ===================== ADD TO CART =====================

	[HttpPost("add-to-cart")]
	public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(request.ASIN))
				return BadRequest(new { success = false, message = "ASIN is required" });

			if (request.Quantity < 1)
				return BadRequest(new { success = false, message = "Quantity must be at least 1" });

			// The IAmazonBuyerService interface does not define GenerateAddToCartUrl.
			// You need to implement this functionality in your service and interface.
			// For now, you can return a NotImplemented result or ask for the correct method.
			return StatusCode(501, new { success = false, message = "Add to cart functionality is not implemented in the service." });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { success = false, message = ex.Message });
		}
	}

	// ===================== BUYER ORDERS =====================

	[HttpGet("orders")]
	public async Task<IActionResult> GetOrders()
	{
		try
		{
			var buyerId = GetUserId();
				await _buyerService.GetBuyerOrdersAsync(buyerId);
			// Since GetBuyerOrdersAsync returns Task (void), there is no result to assign.
			// If you expect data, update the service to return Task<List<Order>> or similar.
			return Ok(new { success = true });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { success = false, message = ex.Message });
		}
	}
}
