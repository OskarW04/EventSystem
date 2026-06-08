using System.Security.Claims;
using EventSystem.API.Common;
using EventSystem.API.DTOs;
using EventSystem.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService) => _userService = userService;

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await _userService.GetUserProfileAsync(userId);

        return profile != null
            ? Ok(ApiResponse<object>.Ok(profile))
            : NotFound(ApiResponse.Fail("Nie znaleziono użytkownika"));
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var success = await _userService.UpdateProfileAsync(userId, dto);

        return success
            ? Ok(ApiResponse.Ok("Profil został zaktualizowany"))
            : BadRequest(ApiResponse.Fail("Nie udało się zaktualizować profilu"));
    }

    [Authorize]
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, error) = await _userService.ChangePasswordAsync(userId, dto);

        return success
            ? Ok(ApiResponse.Ok("Hasło zostało zmienione"))
            : BadRequest(ApiResponse.Fail(error ?? "Nie udało się zmienić hasła"));
    }

    [Authorize]
    [HttpPut("details")]
    public async Task<IActionResult> UpdateUserDetails(UpdateUserDetailsDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, error) = await _userService.UpdateUserDetailsAsync(userId, dto);

        return success
            ? Ok(ApiResponse.Ok("Dane zostały zaktualizowane"))
            : BadRequest(ApiResponse.Fail(error ?? "Nie udało się zaktualizować danych"));
    }

    [Authorize]
    [HttpDelete]
    public async Task<IActionResult> DeleteAccount(DeleteAccountDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, error) = await _userService.DeleteAccountAsync(userId, dto.Password);

        if (success)
        {
            Response.Cookies.Delete("X-Access-Token");
            Response.Cookies.Delete("X-Refresh-Token", new CookieOptions
            {
                Path = "/api/auth/refresh"
            });
            return Ok(ApiResponse.Ok("Konto zostało usunięte"));
        }

        return BadRequest(ApiResponse.Fail(error ?? "Nie udało się usunąć konta"));
    }

    [HttpGet("public/{userId:int}")]
    public async Task<IActionResult> GetPublicProfile(int userId)
    {
        var profile = await _userService.GetPublicProfileAsync(userId);

        return profile != null
            ? Ok(ApiResponse<object>.Ok(profile))
            : NotFound(ApiResponse.Fail("Nie znaleziono użytkownika"));
    }
}