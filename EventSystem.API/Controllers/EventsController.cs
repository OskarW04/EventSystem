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

    // Pobiera listę wszystkich nadchodzących wydarzeń
    [HttpGet]
    public async Task<IActionResult> GetAllUpcoming()
    {
        return Ok(await _eventService.GetUpcomingEventsAsync());
    }

    // Pobiera szczegóły konkretnego wydarzenia - DOSTĘPNE DLA KAŻDEGO ZALOGOWANEGO
    [Authorize] 
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        // Używamy GetEventWithTicketsAsync zamiast PublicAsync, żeby mieć pewność, 
        // że dociągamy wszystkie potrzebne dane (jak EnrolledCount)
        var ev = await _eventService.GetEventWithTicketsAsync(id, 0); 
        if (ev == null) return NotFound("Wydarzenie nie istnieje.");
        
        return Ok(new
        {
            ev.Id,
            ev.Title,
            ev.Description,
            ev.Date,
            ev.Location,
            ev.MaxCapacity,
            EnrolledCount = ev.Tickets?.Count ?? 0 // Obliczamy licznik na bieżąco
        });
    }

    // Tworzenie wydarzenia - zostawiamy dla Organizatora/Admina
    [Authorize(Roles = "Organizer,Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateEvent(CreateEventDto dto)
    {
        var organizerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var eventId = await _eventService.CreateEventAsync(dto, organizerId);
        return Ok(new { EventId = eventId });
    }

    // Pobiera listę uczestników - DOSTĘPNE DLA KAŻDEGO ZALOGOWANEGO (zdjęta blokada roli)
    [Authorize]
    [HttpGet("{id}/attendees")]
    public async Task<IActionResult> GetEventAttendees(int id)
    {
        // Pobieramy dane bez sprawdzania, czy Ty stworzyłeś to wydarzenie
        var ev = await _eventService.GetEventWithTicketsAsync(id, 0); 
        if (ev == null) return NotFound("Wydarzenie nie istnieje.");

        var attendees = ev.Tickets.Select(t => new
        {
            Id = t.Id,
            StudentEmail = t.Student?.Email ?? t.Student?.FirstName ?? "Brak danych",
            RegistrationDate = t.CreatedAt,
            IsUsed = t.IsScanned
        });

        return Ok(attendees);
    }
}