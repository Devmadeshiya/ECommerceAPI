using ECommerceAPI.Models;
using ECommerceAPI.Services;
using ECommerceAPI.src.ECommerceAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
	private readonly IAuthService _authService;

	public AuthController(IAuthService authService)
	{
		_authService = authService;
	}

	/// <summary>
	/// Register a new user (Seller or Buyer)
	/// </summary>
	[HttpPost("register")]
	public async Task<IActionResult> Register([FromBody] RegisterRequest request)
	{
		if (!ModelState.IsValid)
			return BadRequest(ModelState);

		if (!request.Role.Equals("Seller", StringComparison.OrdinalIgnoreCase) &&
			!request.Role.Equals("Buyer", StringComparison.OrdinalIgnoreCase))
		{
			return BadRequest(new { message = "Role must be either 'Seller' or 'Buyer'" });
		}

		var result = await _authService.RegisterAsync(request);

		if (!result.Success)
			return BadRequest(result);

		return Ok(result);
	}

	/// <summary>
	/// Login with email and password
	/// </summary>
	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginRequest request)
	{
		if (!ModelState.IsValid)
			return BadRequest(ModelState);

		var result = await _authService.LoginAsync(request);

		if (!result.Success)
			return Unauthorized(result);

		return Ok(result);
	}
}