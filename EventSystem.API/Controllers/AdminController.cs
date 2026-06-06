// using System.Security.Claims;
// using EventSystem.API.Services;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;

// namespace EventSystem.API.Controllers;

// [ApiController]
// [Route("api/[controller]")]
// //[Authorize(Roles = "Admin")]
// public class AdminController : ControllerBase
// {
//     private readonly SystemAdminService _adminService;
//     public AdminController(SystemAdminService adminService) => _adminService = adminService;

//     [HttpPost("generate-token")]
//     public async Task<IActionResult> GenerateToken()
//     {
//         var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
//         var token = await _adminService.GenerateOrganizationTokenAsync(adminId);
//         return Ok(new { Token = token });
//     }
// }

using System.Security.Claims;
using EventSystem.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize(Roles = "Admin")] // <--- Nadal zakomentowane
public class AdminController : ControllerBase
{
    private readonly SystemAdminService _adminService;
    public AdminController(SystemAdminService adminService) => _adminService = adminService;

    [HttpPost("generate-token")]
    public async Task<IActionResult> GenerateToken()
    {
        // Zakomentowaliśmy oryginalne pobieranie ID, bo nie jesteśmy zalogowani:
        // var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        // HACK: Wpisujemy na sztywno ID admina (np. 1), żeby metoda przeszła dalej
        var adminId = 1; 

        var token = await _adminService.GenerateOrganizationTokenAsync(adminId);
        return Ok(new { Token = token });
    }
}