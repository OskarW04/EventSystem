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
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var success = await _userService.UpdateProfileAsync(userId, dto);

        return success
            ? Ok(ApiResponse.Ok("Profil został zaktualizowany"))
            : BadRequest(ApiResponse.Fail("Nie udało się zaktualizować profilu"));
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