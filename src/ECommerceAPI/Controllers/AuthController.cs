using ECommerceAPI.Models;
using ECommerceAPI.Services;
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
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	public async Task<IActionResult> Register([FromBody] RegisterRequest request)
	{
		if (!ModelState.IsValid)
			return BadRequest(ModelState);

		if (!string.Equals(request.Role, "Seller", StringComparison.OrdinalIgnoreCase) &&
			!string.Equals(request.Role, "Buyer", StringComparison.OrdinalIgnoreCase))
		{
			return BadRequest(new
			{
				success = false,
				message = "Role must be either 'Seller' or 'Buyer'"
			});
		}

		try
		{
			var result = await _authService.RegisterAsync(request);

			if (!result.Success)
				return BadRequest(result);

			return Ok(result);
		}
		catch (Exception ex)
		{
			return StatusCode(500, new
			{
				success = false,
				message = "An error occurred while registering user",
				error = ex.Message
			});
		}
	}

	/// <summary>
	/// Login with email and password
	/// </summary>
	[HttpPost("login")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	public async Task<IActionResult> Login([FromBody] LoginRequest request)
	{
		if (!ModelState.IsValid)
			return BadRequest(ModelState);

		try
		{
			var result = await _authService.LoginAsync(request);

			if (!result.Success)
				return Unauthorized(result);

			return Ok(result);
		}
		catch (Exception ex)
		{
			return StatusCode(500, new
			{
				success = false,
				message = "An error occurred while logging in",
				error = ex.Message
			});
		}
	}
}
