using EventSystem.API.DTOs;
using EventSystem.Core.Data;
using EventSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventSystem.API.Services;

// Wynik próby zapisu na powiadomienie (#4). Pozwala kontrolerowi rozróżnić
// duplikat (409) od pozostałych odrzuceń reguł okna presave (400).
public enum PresaveOutcome
{
    Created,
    AlreadyPresaved,
    Invalid
}

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
            RegistrationOpensAt = dto.RegistrationOpensAt?.ToUniversalTime(),
            PresaveOpensAt = dto.PresaveOpensAt?.ToUniversalTime(),
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

        ev.ImageUrl = await EventImageStorage.SaveAsync(image);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<EventDto>> GetUpcomingEventsAsync(int? studentId)
    {
        var clicksCutoff = DateTime.UtcNow.AddHours(-24);
        var sid = studentId ?? 0;
        var hasUser = studentId.HasValue;

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
                e.Tickets.Count,
                e.Views.Count(v => v.CreatedAt >= clicksCutoff),
                e.RegistrationOpensAt,
                e.PresaveOpensAt,
                hasUser && e.Presaves.Any(p => p.StudentId == sid)))
            .ToListAsync();
    }

    public async Task<Event?> GetEventWithTicketsAsync(int eventId, int organizerId)
    {
        return await _context.Events
            .Include(e => e.Tickets)
                .ThenInclude(t => t.Student)
            .FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == organizerId);
    }

    public async Task<EventDetailsDto?> GetEventDetailsAsync(int eventId, int? studentId)
    {
        var clicksCutoff = DateTime.UtcNow.AddHours(-24);
        var sid = studentId ?? 0;
        var hasUser = studentId.HasValue;

        return await _context.Events
            .AsNoTracking()
            .Where(e => e.Id == eventId)
            .Select(e => new EventDetailsDto(
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
                e.Tickets.Count,
                e.OrganizerId,
                e.Organizer.FirstName,
                e.Organizer.LastName,
                e.Tickets.Count >= e.MaxCapacity,
                e.Views.Count(v => v.CreatedAt >= clicksCutoff),
                e.RegistrationOpensAt,
                e.PresaveOpensAt,
                hasUser && e.Presaves.Any(p => p.StudentId == sid)))
            .FirstOrDefaultAsync();
    }

    public async Task<List<OrganizerEventDto>> GetOrganizerEventsAsync(int organizerId)
    {
        var clicksCutoff = DateTime.UtcNow.AddHours(-24);

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
                e.ImageUrl,
                e.Views.Count(v => v.CreatedAt >= clicksCutoff),
                e.RegistrationOpensAt,
                e.PresaveOpensAt,
                e.Presaves.Count
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
        ev.RegistrationOpensAt = dto.RegistrationOpensAt?.ToUniversalTime();
        ev.PresaveOpensAt = dto.PresaveOpensAt?.ToUniversalTime();

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

    // #6/#5 - rejestruje odsłonę wydarzenia. Deduplikacja per klient (hash IP)
    // w oknie 30 minut, żeby ograniczyć spam. Operacja best-effort - jeśli
    // wydarzenie nie istnieje, po prostu nic nie zapisujemy.
    public async Task RecordViewAsync(int eventId, string? clientKey)
    {
        var exists = await _context.Events.AnyAsync(e => e.Id == eventId);
        if (!exists)
            return;

        if (clientKey != null)
        {
            var dedupCutoff = DateTime.UtcNow.AddMinutes(-30);
            var recentlySeen = await _context.EventViews.AnyAsync(v =>
                v.EventId == eventId &&
                v.ClientKey == clientKey &&
                v.CreatedAt >= dedupCutoff);

            if (recentlySeen)
                return;
        }

        _context.EventViews.Add(new EventView
        {
            EventId = eventId,
            ClientKey = clientKey,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    // #4 - zapis na powiadomienie o starcie rejestracji ("presave"). NIE tworzy
    // biletu ani miejsca - tylko subskrypcja maila. Reguły okna wymuszane
    // serwerowo, żeby nie dało się obejść ukrytego przycisku.
    public async Task<(PresaveOutcome Outcome, string? Error)> AddPresaveAsync(int eventId, int studentId)
    {
        var now = DateTime.UtcNow;

        var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
        if (ev == null)
            return (PresaveOutcome.Invalid, "Podane wydarzenie nie istnieje");

        if (ev.OrganizerId == studentId)
            return (PresaveOutcome.Invalid, "Nie możesz zapisać się na własne wydarzenie");

        if (ev.Date <= now)
            return (PresaveOutcome.Invalid, "To wydarzenie już się odbyło");

        // Presave ma sens tylko zanim ruszy rejestracja. Gdy już otwarta - bilet.
        if (ev.RegistrationOpensAt == null || now >= ev.RegistrationOpensAt.Value)
            return (PresaveOutcome.Invalid, "Rejestracja jest już otwarta — odbierz bilet");

        // Okno presave musiało wystartować (null = dostępne od razu).
        if (ev.PresaveOpensAt.HasValue && now < ev.PresaveOpensAt.Value)
            return (PresaveOutcome.Invalid, "Pre-rejestracja jeszcze się nie rozpoczęła");

        var already = await _context.EventPresaves
            .AnyAsync(p => p.EventId == eventId && p.StudentId == studentId);

        if (already)
            return (PresaveOutcome.AlreadyPresaved, null);

        _context.EventPresaves.Add(new EventPresave
        {
            EventId = eventId,
            StudentId = studentId,
            CreatedAt = now
        });
        await _context.SaveChangesAsync();

        return (PresaveOutcome.Created, null);
    }

    // #4 - wycofanie pre-save. Idempotentne.
    public async Task RemovePresaveAsync(int eventId, int studentId)
    {
        var presave = await _context.EventPresaves
            .FirstOrDefaultAsync(p => p.EventId == eventId && p.StudentId == studentId);

        if (presave != null)
        {
            _context.EventPresaves.Remove(presave);
            await _context.SaveChangesAsync();
        }
    }
}