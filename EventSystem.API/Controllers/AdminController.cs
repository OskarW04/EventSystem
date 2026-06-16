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

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserDto dto)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (user, error) = await _adminService.CreateUserAsync(adminId, dto);

        return user != null
            ? Ok(ApiResponse<object>.Ok(user, "Użytkownik został utworzony"))
            : BadRequest(ApiResponse.Fail(error ?? "Nie udało się utworzyć użytkownika"));
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

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents()
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var events = await _adminService.GetAllEventsAsync(adminId);
        return Ok(ApiResponse<object>.Ok(events));
    }

    [HttpGet("events/{eventId:int}")]
    public async Task<IActionResult> GetEvent(int eventId)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ev = await _adminService.GetEventDetailsAsync(adminId, eventId);

        return ev != null
            ? Ok(ApiResponse<object>.Ok(ev))
            : NotFound(ApiResponse.Fail("Wydarzenie nie istnieje"));
    }

    [HttpPut("events/{eventId:int}")]
    public async Task<IActionResult> UpdateEvent(int eventId, [FromBody] UpdateEventDto dto)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, error) = await _adminService.UpdateEventAsync(adminId, eventId, dto);

        return success
            ? Ok(ApiResponse.Ok("Wydarzenie zostało zaktualizowane"))
            : BadRequest(ApiResponse.Fail(error ?? "Nie udało się zaktualizować wydarzenia"));
    }

    [HttpPost("events/{eventId:int}/upload-image")]
    public async Task<IActionResult> UploadEventImage(int eventId, IFormFile image)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, error) = await _adminService.UploadEventImageAsync(adminId, eventId, image);

        return success
            ? Ok(ApiResponse.Ok("Zdjęcie zostało zapisane"))
            : BadRequest(ApiResponse.Fail(error ?? "Nie udało się zapisać zdjęcia. Sprawdź czy plik jest prawidłowy"));
    }

    [HttpDelete("events/{eventId:int}")]
    public async Task<IActionResult> DeleteEvent(int eventId)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, error) = await _adminService.DeleteEventAsync(adminId, eventId);

        return success
            ? Ok(ApiResponse.Ok("Wydarzenie zostało usunięte"))
            : BadRequest(ApiResponse.Fail(error ?? "Nie udało się usunąć wydarzenia"));
    }

    [HttpGet("events/{eventId:int}/attendees")]
    public async Task<IActionResult> GetEventAttendees(int eventId)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var attendees = await _adminService.GetEventAttendeesAsync(adminId, eventId);

        return attendees != null
            ? Ok(ApiResponse<object>.Ok(attendees))
            : NotFound(ApiResponse.Fail("Wydarzenie nie istnieje"));
    }

    [HttpPost("tickets/{scanToken:guid}/reset")]
    public async Task<IActionResult> ResetTicket(Guid scanToken)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, error) = await _adminService.ResetTicketScanAsync(adminId, scanToken);

        return success
            ? Ok(ApiResponse.Ok("Skan biletu został zresetowany"))
            : BadRequest(ApiResponse.Fail(error ?? "Nie udało się zresetować biletu"));
    }

    [HttpPost("tickets/{scanToken:guid}/scan")]
    public async Task<IActionResult> ScanTicket(Guid scanToken)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (result, error) = await _adminService.ScanTicketAsync(adminId, scanToken);

        return error != null
            ? BadRequest(ApiResponse.Fail(error))
            : Ok(ApiResponse<object>.Ok(result!, "Bilet został zweryfikowany pomyślnie"));
    }

    [HttpDelete("tickets/{ticketId:int}")]
    public async Task<IActionResult> DeleteTicket(int ticketId)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, error) = await _adminService.DeleteTicketAsync(adminId, ticketId);

        return success
            ? Ok(ApiResponse.Ok("Bilet został usunięty, miejsce zwolnione"))
            : BadRequest(ApiResponse.Fail(error ?? "Nie udało się usunąć biletu"));
    }

    [HttpPost("send-token-email")]
    public async Task<IActionResult> SendTokenEmail([FromBody] SendTokenEmailDto dto)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, error) = await _adminService.SendTokenEmailAsync(adminId, dto.Token, dto.Email);

        return success
            ? Ok(ApiResponse.Ok($"Token został wysłany na adres {dto.Email}"))
            : BadRequest(ApiResponse.Fail(error ?? "Nie udało się wysłać wiadomości"));
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] int limit = 100)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var logs = await _adminService.GetLogsAsync(adminId, limit);
        return Ok(ApiResponse<object>.Ok(logs));
    }
}