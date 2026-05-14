using System.Security.Claims;
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
        return Ok(await _eventService.GetUpcomingEventsAsync());
    }

    [Authorize(Roles = "Organizer")]
    [HttpPost]
    public async Task<IActionResult> CreateEvent(CreateEventDto dto)
    {
        var organizerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var eventId = await _eventService.CreateEventAsync(dto, organizerId);
        return Ok(new { EventId = eventId });
    }

    [Authorize(Roles = "Organizer")]
    [HttpPost("{id}/upload-image")]
    public async Task<IActionResult> UploadImage(int id, IFormFile image)
    {
        var organizerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var success = await _eventService.UploadEventImageAsync(id, organizerId, image);
        return success ? Ok() : BadRequest("Błąd zapisu zdjęcia.");
    }

    [Authorize(Roles = "Organizer")]
    [HttpGet("{id}/attendees")]
    public async Task<IActionResult> GetEventAttendees(int id)
    {
        var organizerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var ev = await _eventService.GetEventWithTicketsAsync(id, organizerId); // Będziesz musiał dopisać tę metodę w serwisie
        if (ev == null) return NotFound("Wydarzenie nie istnieje lub nie masz dostępu.");

        var attendees = ev.Tickets.Select(t => new
        {
            t.Id,
            t.Student.FirstName,
            t.Student.LastName,
            t.IsScanned
        });

        return Ok(attendees);
    }
}