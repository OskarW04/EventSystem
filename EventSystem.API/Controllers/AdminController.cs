using System.Security.Claims;
using EventSystem.API.Common;
using EventSystem.API.DTOs;
using EventSystem.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly SystemAdminService _adminService;

    public AdminController(SystemAdminService adminService) => _adminService = adminService;

    [HttpPost("generate-token")]
    public async Task<IActionResult> GenerateToken()
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var token = await _adminService.GenerateOrganizationTokenAsync(adminId);
        return Ok(ApiResponse<object>.Ok(new { token }, "Token został wygenerowany"));
    }

    [HttpPost("revoke-token")]
    public async Task<IActionResult> RevokeToken([FromBody] string tokenValue)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var success = await _adminService.RevokeOrganizationTokenAsync(adminId, tokenValue);

        return success
            ? Ok(ApiResponse.Ok("Token został unieważniony"))
            : BadRequest(ApiResponse.Fail("Nie udało się unieważnić tokena"));
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var users = await _adminService.GetAllUsersAsync(adminId);
        return Ok(ApiResponse<object>.Ok(users));
    }

    [HttpDelete("users/{userId:int}")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var success = await _adminService.DeleteUserAsync(adminId, userId);

        return success
            ? Ok(ApiResponse.Ok("Użytkownik został usunięty"))
            : BadRequest(ApiResponse.Fail("Nie udało się usunąć użytkownika"));
    }

    [HttpPut("users/{userId:int}/role")]
    public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] UpdateRoleDto dto)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var success = await _adminService.UpdateUserRoleAsync(adminId, userId, dto.NewRole);

        return success
            ? Ok(ApiResponse.Ok("Rola użytkownika została zaktualizowana"))
            : BadRequest(ApiResponse.Fail("Nie udało się zaktualizować roli użytkownika"));
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] int limit = 100)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var logs = await _adminService.GetLogsAsync(adminId, limit);
        return Ok(ApiResponse<object>.Ok(logs));
    }
}