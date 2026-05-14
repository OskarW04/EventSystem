using System.Security.Claims;
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
    [HttpPost("enroll/{eventId}")]
    public async Task<IActionResult> Enroll(int eventId)
    {
        var studentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _ticketService.EnrollInEventAsync(eventId, studentId);
        return result == "Success" ? Ok() : BadRequest(result);
    }

    [Authorize(Roles = "Student")]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyTickets()
    {
        var studentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _ticketService.GetMyTicketsAsync(studentId));
    }

    [Authorize(Roles = "Organizer")]
    [HttpPost("scan/{ticketId}")]
    public async Task<IActionResult> ScanTicket(int ticketId)
    {
        var organizerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _ticketService.ScanTicketAsync(ticketId, organizerId);
        return result == "Success" ? Ok() : BadRequest(result);
    }
}