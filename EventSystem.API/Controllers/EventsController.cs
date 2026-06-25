using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EventSystem.API.Common;
using EventSystem.API.DTOs;
using EventSystem.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly EventService _eventService;

    public EventsController(EventService eventService) => _eventService = eventService;

    [HttpGet]
    public async Task<IActionResult> GetAllUpcoming()
    {
        var events = await _eventService.GetUpcomingEventsAsync(GetOptionalUserId());
        return Ok(ApiResponse<object>.Ok(events));
    }

    [Authorize(Roles = "Organizer")]
    [HttpPost]
    public async Task<IActionResult> CreateEvent(CreateEventDto dto)
    {
        var organizerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var eventId = await _eventService.CreateEventAsync(dto, organizerId);
        return Ok(ApiResponse<object>.Ok(new { eventId }, "Wydarzenie zostało utworzone"));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEventDetails(int id)
    {
        var eventDetails = await _eventService.GetEventDetailsAsync(id, GetOptionalUserId());

        return eventDetails != null
            ? Ok(ApiResponse<object>.Ok(eventDetails))
            : NotFound(ApiResponse.Fail("Wydarzenie nie istnieje"));
    }

    // #6/#5 - zliczenie odsłony. Fire-and-forget, bez autoryzacji, zawsze 204.
    [HttpPost("{id:int}/view")]
    public async Task<IActionResult> RecordView(int id)
    {
        await _eventService.RecordViewAsync(id, GetClientKey());
        return NoContent();
    }

    // #4 - presave: zapis na powiadomienie mailowe o starcie rejestracji.
    // Nie tworzy biletu ani miejsca - tylko subskrypcja maila.
    [Authorize(Roles = "Student")]
    [HttpPost("{eventId:int}/presave")]
    public async Task<IActionResult> Presave(int eventId)
    {
        var studentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (outcome, error) = await _eventService.AddPresaveAsync(eventId, studentId);

        return outcome switch
        {
            PresaveOutcome.Created => Ok(ApiResponse<object>.Ok(null!, "Zapisano na powiadomienie")),
            PresaveOutcome.AlreadyPresaved => Conflict(new { error = "already_presaved" }),
            _ => BadRequest(ApiResponse.Fail(error ?? "Nie udało się zapisać na powiadomienie"))
        };
    }

    // #4 - rezygnacja z powiadomienia. Idempotentne, zwraca ciało JSON.
    [Authorize(Roles = "Student")]
    [HttpDelete("{eventId:int}/presave")]
    public async Task<IActionResult> CancelPresave(int eventId)
    {
        var studentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _eventService.RemovePresaveAsync(eventId, studentId);
        return Ok(ApiResponse.Ok("Anulowano zapis na powiadomienie"));
    }

    [Authorize(Roles = "Organizer")]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyEvents()
    {
        var organizerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var events = await _eventService.GetOrganizerEventsAsync(organizerId);
        return Ok(ApiResponse<object>.Ok(events));
    }

    [Authorize(Roles = "Organizer")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateEvent(int id, UpdateEventDto dto)
    {
        var organizerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, error) = await _eventService.UpdateEventAsync(id, organizerId, dto);

        return success
            ? Ok(ApiResponse.Ok("Wydarzenie zostało zaktualizowane"))
            : BadRequest(ApiResponse.Fail(error ?? "Nie udało się zaktualizować wydarzenia"));
    }

    [Authorize(Roles = "Organizer")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var organizerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, error) = await _eventService.DeleteEventAsync(id, organizerId);

        return success
            ? Ok(ApiResponse.Ok("Wydarzenie zostało usunięte"))
            : BadRequest(ApiResponse.Fail(error ?? "Nie udało się usunąć wydarzenia"));
    }

    [Authorize(Roles = "Organizer")]
    [HttpPost("{id:int}/upload-image")]
    public async Task<IActionResult> UploadImage(int id, IFormFile image)
    {
        var organizerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var success = await _eventService.UploadEventImageAsync(id, organizerId, image);

        return success
            ? Ok(ApiResponse.Ok("Zdjęcie zostało zapisane"))
            : BadRequest(ApiResponse.Fail("Nie udało się zapisać zdjęcia. Sprawdź czy plik jest prawidłowy"));
    }

    [Authorize(Roles = "Organizer")]
    [HttpGet("{id:int}/attendees")]
    public async Task<IActionResult> GetEventAttendees(int id)
    {
        var organizerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ev = await _eventService.GetEventWithTicketsAsync(id, organizerId);

        if (ev == null)
            return NotFound(ApiResponse.Fail("Wydarzenie nie istnieje lub nie masz do niego dostępu"));

        var attendees = ev.Tickets.Select(t => new
        {
            t.Id,
            t.ScanToken,
            StudentEmail = t.Student.Email,
            t.Student.FirstName,
            t.Student.LastName,
            t.IsScanned
        });

        return Ok(ApiResponse<object>.Ok(attendees));
    }

    // Zwraca id zalogowanego użytkownika lub null dla anonimowych żądań
    // (endpointy publiczne, na których token może, ale nie musi być obecny).
    private int? GetOptionalUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var id) ? id : null;
    }

    // Stabilny, zanonimizowany klucz klienta (hash IP) do deduplikacji odsłon.
    private string? GetClientKey()
    {
        var ip = Request.Headers.TryGetValue("X-Forwarded-For", out var fwd)
            && !string.IsNullOrWhiteSpace(fwd)
                ? fwd.ToString().Split(',')[0].Trim()
                : HttpContext.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrWhiteSpace(ip))
            return null;

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(ip));
        return Convert.ToHexString(hash);
    }
}