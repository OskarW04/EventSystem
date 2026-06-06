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
            Date = dto.Date.ToUniversalTime(),
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

   // ... reszta importów i metod ...

public async Task<List<object>> GetUpcomingEventsAsync()
{
    return await _context.Events
        .AsNoTracking()
        .Include(e => e.Tickets)
        .Where(e => e.Date >= DateTime.UtcNow.AddDays(-1))
        .Select(e => new {
            Id = e.Id,
            Title = e.Title,
            Description = e.Description,
            Date = e.Date,
            Location = e.Location,
            MaxCapacity = e.MaxCapacity,
            ImageUrl = e.ImageUrl,
            EnrolledCount = e.Tickets.Count // Dopasowane do Twojego Swaggera
        })
        .ToListAsync<object>();
}

public async Task<object?> GetEventByIdPublicAsync(int eventId)
{
    return await _context.Events
        .AsNoTracking()
        .Include(e => e.Tickets)
        .Where(e => e.Id == eventId)
        .Select(e => new {
            Id = e.Id,
            Title = e.Title,
            Description = e.Description,
            Date = e.Date,
            Location = e.Location,
            MaxCapacity = e.MaxCapacity,
            ImageUrl = e.ImageUrl,
            EnrolledCount = e.Tickets.Count
        })
        .FirstOrDefaultAsync();
}

// Ta metoda jest potrzebna dla listy uczestników
// Podmień tę metodę w EventService.cs
    public async Task<Event?> GetEventWithTicketsAsync(int eventId, int organizerId)
    {
        // Usuwamy sprawdzanie organizerId, zostawiamy tylko ID wydarzenia
        return await _context.Events
            .Include(e => e.Tickets)
                .ThenInclude(t => t.Student)
            .FirstOrDefaultAsync(e => e.Id == eventId); 
    }
}