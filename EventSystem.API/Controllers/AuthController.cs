using EventSystem.API.DTOs;
using EventSystem.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly IConfiguration _config;

    public AuthController(AuthService authService, IConfiguration config)
    {
        _authService = authService;
        _config = config;
    }

    [HttpPost("register/student")]
    public async Task<IActionResult> RegisterStudent(RegisterStudentDto dto)
    {
        var success = await _authService.RegisterStudentAsync(dto);
        return success
            ? Ok(new { message = "Konto studenta zostało utworzone" })
            : BadRequest(new { message = "Rejestracja nie powiodła się : podany adres e-mail może być już zajęty" });
    }

    [HttpPost("register/organizer")]
    public async Task<IActionResult> RegisterOrganizer(RegisterOrganizerDto dto)
    {
        var result = await _authService.RegisterOrganizerAsync(dto);
        return result == "Success"
            ? Ok(new { message = "Konto organizatora zostało utworzone" })
            : BadRequest(new { message = result });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var (response, error) = await _authService.LoginAsync(dto);

        if (error != null)
            return Unauthorized(new { message = error });

        var accessTokenExpiry = int.Parse(
            _config["JwtSettings:AccessTokenExpiryMinutes"] ?? "15");
        var refreshTokenExpiry = int.Parse(
            _config["JwtSettings:RefreshTokenExpiryDays"] ?? "30");

        Response.Cookies.Append("X-Access-Token", response!.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(accessTokenExpiry)
        });

        Response.Cookies.Append("X-Refresh-Token", response.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(refreshTokenExpiry),
            Path = "/api/auth/refresh"
        });

        return Ok(new
        {
            role = response.Role,
            userId = response.UserId
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["X-Refresh-Token"];

        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { message = "Brak tokenu odświeżającego" });

        var (response, error) = await _authService.RefreshAsync(refreshToken);

        if (error != null)
            return Unauthorized(new { message = error });

        var accessTokenExpiry = int.Parse(
            _config["JwtSettings:AccessTokenExpiryMinutes"] ?? "15");
        var refreshTokenExpiry = int.Parse(
            _config["JwtSettings:RefreshTokenExpiryDays"] ?? "30");

        Response.Cookies.Append("X-Access-Token", response!.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(accessTokenExpiry)
        });

        Response.Cookies.Append("X-Refresh-Token", response.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(refreshTokenExpiry),
            Path = "/api/auth/refresh"
        });

        return Ok(new { message = "Sesja została odświeżona" });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["X-Refresh-Token"];

        if (!string.IsNullOrEmpty(refreshToken))
            await _authService.RevokeRefreshTokenAsync(refreshToken);

        Response.Cookies.Delete("X-Access-Token");
        Response.Cookies.Delete("X-Refresh-Token", new CookieOptions
        {
            Path = "/api/auth/refresh"
        });

        return Ok(new { message = "Wylogowano pomyślnie" });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        var success = await _authService.InitiatePasswordResetAsync(dto.Email);
        return Ok(new { message = "Jeśli podany adres e-mail istnieje, otrzymasz link do resetowania hasła" });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        var (success, error) = await _authService.ResetPasswordAsync(dto);

        return success
            ? Ok(new { message = "Hasło zostało zresetowane. Możesz się teraz zalogować" })
            : BadRequest(new { message = error ?? "Nie udało się zresetować hasła" });
    }
}