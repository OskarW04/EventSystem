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
            EndDate = dto.EndDate?.ToUniversalTime(),
            Location = dto.Location,
            LocationName = dto.LocationName,
            Lat = dto.Lat,
            Lng = dto.Lng,
            MaxCapacity = dto.MaxCapacity,
            OrganizerId = organizerId
        };

        _context.Events.Add(newEvent);
        await _context.SaveChangesAsync();
        return newEvent.Id;
    }

    public async Task<bool> UploadEventImageAsync(int eventId, int organizerId, IFormFile image)
    {
        var ev = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == organizerId);

        if (ev == null || image.Length == 0)
            return false;

        var uploadsFolder = Path.Combine(
            Directory.GetCurrentDirectory(), "wwwroot", "images", "events");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await image.CopyToAsync(stream);

        ev.ImageUrl = $"/images/events/{fileName}";
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<EventDto>> GetUpcomingEventsAsync()
    {
        return await _context.Events
            .AsNoTracking()
            .Where(e => e.Date >= DateTime.UtcNow)
            .Select(e => new EventDto(
                e.Id,
                e.Title,
                e.Description,
                e.Date,
                e.EndDate,
                e.Location,
                e.LocationName,
                e.Lat,
                e.Lng,
                e.MaxCapacity,
                e.ImageUrl,
                e.Tickets.Count))
            .ToListAsync();
    }

    public async Task<Event?> GetEventWithTicketsAsync(int eventId, int organizerId)
    {
        return await _context.Events
            .Include(e => e.Tickets)
                .ThenInclude(t => t.Student)
            .FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == organizerId);
    }

    public async Task<EventDetailsDto?> GetEventDetailsAsync(int eventId)
    {
        var ev = await _context.Events
            .AsNoTracking()
            .Include(e => e.Organizer)
            .Include(e => e.Tickets)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (ev == null)
            return null;

        return new EventDetailsDto(
            ev.Id,
            ev.Title,
            ev.Description,
            ev.Date,
            ev.EndDate,
            ev.Location,
            ev.LocationName,
            ev.Lat,
            ev.Lng,
            ev.MaxCapacity,
            ev.ImageUrl,
            ev.Tickets.Count,
            ev.OrganizerId,
            ev.Organizer.FirstName,
            ev.Organizer.LastName,
            ev.Tickets.Count >= ev.MaxCapacity
        );
    }

    public async Task<List<OrganizerEventDto>> GetOrganizerEventsAsync(int organizerId)
    {
        return await _context.Events
            .AsNoTracking()
            .Where(e => e.OrganizerId == organizerId)
            .OrderByDescending(e => e.Date)
            .Select(e => new OrganizerEventDto(
                e.Id,
                e.Title,
                e.Date,
                e.EndDate,
                e.Location,
                e.LocationName,
                e.Lat,
                e.Lng,
                e.MaxCapacity,
                e.Tickets.Count,
                e.Tickets.Count(t => t.IsScanned),
                e.ImageUrl
            ))
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error)> UpdateEventAsync(
        int eventId, int organizerId, UpdateEventDto dto)
    {
        var ev = await _context.Events
            .Include(e => e.Tickets)
            .FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == organizerId);

        if (ev == null)
            return (false, "Wydarzenie nie istnieje lub nie masz do niego dostępu");

        if (dto.MaxCapacity < ev.Tickets.Count)
            return (false, $"Nie można zmniejszyć pojemności poniżej {ev.Tickets.Count} (liczba zapisanych uczestników)");

        ev.Title = dto.Title;
        ev.Description = dto.Description;
        ev.Date = dto.Date.ToUniversalTime();
        ev.EndDate = dto.EndDate?.ToUniversalTime();
        ev.Location = dto.Location;
        ev.LocationName = dto.LocationName;
        ev.Lat = dto.Lat;
        ev.Lng = dto.Lng;
        ev.MaxCapacity = dto.MaxCapacity;

        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteEventAsync(int eventId, int organizerId)
    {
        var ev = await _context.Events
            .Include(e => e.Tickets)
            .FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == organizerId);

        if (ev == null)
            return (false, "Wydarzenie nie istnieje lub nie masz do niego dostępu");


        if (ev.Tickets.Any())
            return (false, "Nie można usunąć wydarzenia z zapisanymi uczestnikami");

        if (!string.IsNullOrEmpty(ev.ImageUrl))
        {
            var imagePath = Path.Combine(
                Directory.GetCurrentDirectory(), "wwwroot", ev.ImageUrl.TrimStart('/'));

            if (File.Exists(imagePath))
                File.Delete(imagePath);
        }

        _context.Events.Remove(ev);
        await _context.SaveChangesAsync();
        return (true, null);
    }
}