using EventSystem.API.DTOs;
using EventSystem.Core.Data;
using EventSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventSystem.API.Services;

public class EventService
{
    private readonly AppDbContext _context;
    public EventService(AppDbContext context) => _context = context;

    public async Task<int> CreateEventAsync(CreateEventDto dto, int organizerId)
    {
        var newEvent = new Event
        {
            Title = dto.Title,
            Description = dto.Description,
            Date = dto.Date,
            Location = dto.Location,
            MaxCapacity = dto.MaxCapacity,
            OrganizerId = organizerId
        };
        _context.Events.Add(newEvent);
        await _context.SaveChangesAsync();
        return newEvent.Id;
    }

    public async Task<bool> UploadEventImageAsync(int eventId, int organizerId, IFormFile image)
    {
        var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == organizerId);
        if (ev == null || image.Length == 0) return false;

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "events");
        Directory.CreateDirectory(uploadsFolder);
        var uniqueFileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await image.CopyToAsync(stream);
        }

        ev.ImageUrl = $"/images/events/{uniqueFileName}";
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<EventDto>> GetUpcomingEventsAsync()
    {
        return await _context.Events
            .Where(e => e.Date >= DateTime.UtcNow)
            .Select(e => new EventDto(
                e.Id, e.Title, e.Description, e.Date, e.Location, e.MaxCapacity, e.ImageUrl, e.Tickets.Count))
            .ToListAsync();
    }
    public async Task<Event?> GetEventWithTicketsAsync(int eventId, int organizerId)
    {
        return await _context.Events
            .Include(e => e.Tickets)
                .ThenInclude(t => t.Student)
            .FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == organizerId);
    }

}