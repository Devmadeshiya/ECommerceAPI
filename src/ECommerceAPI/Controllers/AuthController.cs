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

	// ===================== REGISTER =====================

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

		await _authService.RegisterAsync(request);
		// You may need to update the rest of the logic here, since RegisterAsync returns void.
		// For example, you might want to return a status code or message indicating success.
		return Ok(new { Success = true, Message = "Registration successful." });
	}

	// ===================== LOGIN =====================

	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginRequest request)
	{
		if (!ModelState.IsValid)
			return BadRequest(ModelState);

		await _authService.LoginAsync(request).ConfigureAwait(false);
		// Handle the result or response here if needed
		var result = (object)null; // Replace with actual result if LoginAsync returns a value in the future

		if (result == null || !(bool)result.GetType().GetProperty("Success")?.GetValue(result))
			return Unauthorized(result);

		return Ok(result);
	}
}
