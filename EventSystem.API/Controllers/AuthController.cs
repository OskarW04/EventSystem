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

    // 1. Ciasteczko zostaje dla bezpieczeństwa (np. do apiClient)
    // UWAGA: Wyłączyłem 'Secure = true' dla testów na localhost (inaczej przeglądarka może odrzucić ciasteczko)
    Response.Cookies.Append("X-Access-Token", token!, new CookieOptions 
    { 
        HttpOnly = true, 
        SameSite = SameSiteMode.Strict, 
        Expires = DateTime.UtcNow.AddDays(7) 
    });

    // 2. KLUCZOWA ZMIANA: Zwracamy token w body, aby JS mógł go zdekodować i poznać ROLĘ
    return Ok(new { accessToken = token });
}

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("X-Access-Token");
        return Ok();
    }
}