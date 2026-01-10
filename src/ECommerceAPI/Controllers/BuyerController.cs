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

			var products = await _buyerService.SearchProductsAsync(searchRequest);

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
			var product = await _buyerService.GetProductDetailsAsync(asin);

			if (product == null)
				return NotFound(new { success = false, message = "Product not found" });

			return Ok(new { success = true, data = product });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { success = false, message = ex.Message });
		}
	}

	// ===================== ADD TO CART =====================

	[HttpPost("add-to-cart")]
	public IActionResult AddToCart([FromBody] AddToCartRequest request)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(request.ASIN))
				return BadRequest(new { success = false, message = "ASIN is required" });

			if (request.Quantity < 1)
				return BadRequest(new { success = false, message = "Quantity must be at least 1" });

			var result = _buyerService.GenerateAddToCartUrl(request);
			return Ok(result);
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
			var orders = await _buyerService.GetBuyerOrdersAsync(buyerId);
			return Ok(new { success = true, data = orders });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { success = false, message = ex.Message });
		}
	}
}
