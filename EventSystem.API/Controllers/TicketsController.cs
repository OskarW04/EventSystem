using System.Security.Claims;
using EventSystem.API.Common;
using EventSystem.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly TicketService _ticketService;

    public TicketsController(TicketService ticketService) => _ticketService = ticketService;

    [Authorize(Roles = "Student")]
    [HttpPost("enroll/{eventId:int}")]
    public async Task<IActionResult> Enroll(int eventId)
    {
        var studentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (ticket, error, registrationOpensAt) =
            await _ticketService.EnrollInEventAsync(eventId, studentId);

        // #4 - rejestracja jeszcze nieotwarta: 409 z kodem i czasem otwarcia.
        if (error == TicketService.RegistrationNotOpenError)
            return Conflict(new { error, opensAt = registrationOpensAt });

        return error != null
            ? BadRequest(ApiResponse.Fail(error))
            : Ok(ApiResponse<object>.Ok(ticket!, "Zapisano na wydarzenie pomyślnie"));
    }

    [Authorize(Roles = "Student")]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyTickets()
    {
        var studentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var tickets = await _ticketService.GetMyTicketsAsync(studentId);
        return Ok(ApiResponse<object>.Ok(tickets));
    }

    // EXAMPLE: POST /api/tickets/scan/3fa85f64-5717-4562-b3fc-2c963f66afa6
    [Authorize(Roles = "Organizer")]
    [HttpPost("scan/{scanToken:guid}")]
    public async Task<IActionResult> ScanTicket(Guid scanToken)
    {
        var organizerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (result, error) = await _ticketService.ScanTicketAsync(scanToken, organizerId);

        return error != null
            ? BadRequest(ApiResponse.Fail(error))
            : Ok(ApiResponse<object>.Ok(result!, "Bilet został zweryfikowany pomyślnie"));
    }
}