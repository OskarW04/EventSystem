using System.Security.Claims;
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
        var events = await _eventService.GetUpcomingEventsAsync();
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
        var eventDetails = await _eventService.GetEventDetailsAsync(id);

        return eventDetails != null
            ? Ok(ApiResponse<object>.Ok(eventDetails))
            : NotFound(ApiResponse.Fail("Wydarzenie nie istnieje"));
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
}