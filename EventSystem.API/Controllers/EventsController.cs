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
            t.Student.FirstName,
            t.Student.LastName,
            t.IsScanned
        });

        return Ok(ApiResponse<object>.Ok(attendees));
    }
}