using EventSystem.API.DTOs;
using EventSystem.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    public AuthController(AuthService authService) => _authService = authService;

    [HttpPost("register/student")]
    public async Task<IActionResult> RegisterStudent(RegisterStudentDto dto)
    {
        return await _authService.RegisterStudentAsync(dto) ? Ok() : BadRequest("Błąd rejestracji.");
    }

    [HttpPost("register/organizer")]
    public async Task<IActionResult> RegisterOrganizer(RegisterOrganizerDto dto)
    {
        var result = await _authService.RegisterOrganizerAsync(dto);
        return result == "Success" ? Ok() : BadRequest(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var (token, error) = await _authService.LoginAsync(dto);
        if (error != null) return Unauthorized(error);

        Response.Cookies.Append("X-Access-Token", token!, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = DateTime.UtcNow.AddDays(7) });
        return Ok();
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("X-Access-Token");
        return Ok();
    }
}