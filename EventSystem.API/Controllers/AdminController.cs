using System.Security.Claims;
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
        return Ok(new { token });
    }
}