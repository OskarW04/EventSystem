using EventSystem.API.DTOs;
using EventSystem.Core.Data;
using EventSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventSystem.API.Services;

public class SystemAdminService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SystemAdminService> _logger;
    private readonly IEmailService _emailService;

    public SystemAdminService(
        AppDbContext context,
        ILogger<SystemAdminService> logger,
        IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<string> GenerateOrganizationTokenAsync(int adminId)
    {
        var tokenValue = Guid.NewGuid().ToString("N");

        _context.OrganizationTokens.Add(new OrganizationToken
        {
            TokenValue = tokenValue,
            CreatedById = adminId
        });

        await _context.SaveChangesAsync();

        await LogActionAsync(adminId, "GenerateToken", "OrganizationToken", null,
            $"Wygenerowano nowy token organizacji");

        return tokenValue;
    }

    public async Task<bool> RevokeOrganizationTokenAsync(int adminId, string tokenValue)
    {
        try
        {
            var token = await _context.OrganizationTokens
                .FirstOrDefaultAsync(t => t.TokenValue == tokenValue && !t.IsUsed);

            if (token == null)
                return false;

            token.IsUsed = true;
            await _context.SaveChangesAsync();

            await LogActionAsync(adminId, "RevokeToken", "OrganizationToken", token.Id,
                $"Unieważniono token organizacji: {tokenValue}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking organization token");
            return false;
        }
    }

    public async Task<List<UserListDto>> GetAllUsersAsync(int adminId)
    {
        var users = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.CreatedEvents)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserListDto(
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.Role.Name,
                u.CreatedAt,
                u.CreatedEvents.Any(e => e.Date >= DateTime.UtcNow)
            ))
            .ToListAsync();

        await LogActionAsync(adminId, "ViewUsers", "User", null,
            $"Przeglądanie listy użytkowników (liczba: {users.Count})");

        return users;
    }

    public async Task<bool> DeleteUserAsync(int adminId, int userId)
    {
        try
        {
            // Nie można usunąć samego siebie
            if (adminId == userId)
            {
                _logger.LogWarning("Admin {AdminId} attempted to delete their own account", adminId);
                return false;
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.CreatedEvents)
                    .ThenInclude(e => e.Tickets)
                .Include(u => u.Tickets)
                .Include(u => u.SocialLinks)
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return false;

            // Nie można usunąć innego admina
            if (user.Role.Name == "Admin")
            {
                _logger.LogWarning("Admin {AdminId} attempted to delete another admin {UserId}",
                    adminId, userId);
                return false;
            }

            var userEmail = user.Email;

            // Usuń wszystkie powiązane dane
            _context.SocialLinks.RemoveRange(user.SocialLinks);
            _context.RefreshTokens.RemoveRange(user.RefreshTokens);
            _context.Tickets.RemoveRange(user.Tickets);

            // Odepnij zużyte tokeny organizacyjne (token pozostaje zużyty)
            var usedTokens = await _context.OrganizationTokens
                .Where(t => t.UsedById == userId)
                .ToListAsync();
            foreach (var token in usedTokens)
                token.UsedById = null;

            // Usuń obrazy wydarzeń
            foreach (var ev in user.CreatedEvents.Where(e => !string.IsNullOrEmpty(e.ImageUrl)))
            {
                var imagePath = Path.Combine(
                    Directory.GetCurrentDirectory(), "wwwroot", ev.ImageUrl!.TrimStart('/'));

                if (File.Exists(imagePath))
                    File.Delete(imagePath);
            }

            _context.Events.RemoveRange(user.CreatedEvents);
            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            await LogActionAsync(adminId, "DeleteUser", "User", userId,
                $"Usunięto użytkownika: {userEmail}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> UpdateUserRoleAsync(int adminId, int userId, string newRoleName)
    {
        try
        {
            // Nie można zmienić roli samemu sobie
            if (adminId == userId)
            {
                _logger.LogWarning("Admin {AdminId} attempted to change their own role", adminId);
                return false;
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return false;

            // Nie można zmienić roli innemu adminowi
            if (user.Role.Name == "Admin")
            {
                _logger.LogWarning("Admin {AdminId} attempted to change role of another admin {UserId}",
                    adminId, userId);
                return false;
            }

            var newRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == newRoleName);

            if (newRole == null || newRole.Name == "Admin")
            {
                _logger.LogWarning("Invalid role requested: {RoleName}", newRoleName);
                return false;
            }

            var oldRoleName = user.Role.Name;
            user.RoleId = newRole.Id;

            await _context.SaveChangesAsync();

            await LogActionAsync(adminId, "UpdateRole", "User", userId,
                $"Zmieniono rolę użytkownika {user.Email} z {oldRoleName} na {newRoleName}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user role for {UserId}", userId);
            return false;
        }
    }

    public async Task<List<AdminEventDto>> GetAllEventsAsync(int adminId)
    {
        var events = await _context.Events
            .AsNoTracking()
            .Include(e => e.Organizer)
            .OrderByDescending(e => e.Date)
            .Select(e => new AdminEventDto(
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
                e.Tickets.Count(t => t.IsScanned),
                e.OrganizerId,
                e.Organizer.FirstName,
                e.Organizer.LastName,
                e.Organizer.Email,
                e.Organizer.FirstName + " " + e.Organizer.LastName
            ))
            .ToListAsync();

        await LogActionAsync(adminId, "ViewEvents", "Event", null,
            $"Przeglądanie listy wydarzeń (liczba: {events.Count})");

        return events;
    }

    public async Task<AdminEventDto?> GetEventDetailsAsync(int adminId, int eventId)
    {
        var ev = await _context.Events
            .AsNoTracking()
            .Include(e => e.Organizer)
            .Where(e => e.Id == eventId)
            .Select(e => new AdminEventDto(
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
                e.Tickets.Count(t => t.IsScanned),
                e.OrganizerId,
                e.Organizer.FirstName,
                e.Organizer.LastName,
                e.Organizer.Email,
                e.Organizer.FirstName + " " + e.Organizer.LastName
            ))
            .FirstOrDefaultAsync();

        return ev;
    }

    public async Task<(bool Success, string? Error)> UpdateEventAsync(
        int adminId, int eventId, UpdateEventDto dto)
    {
        try
        {
            var ev = await _context.Events
                .Include(e => e.Tickets)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev == null)
                return (false, "Wydarzenie nie istnieje");

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

            await LogActionAsync(adminId, "UpdateEvent", "Event", eventId,
                $"Zaktualizowano wydarzenie: {ev.Title}");

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event {EventId}", eventId);
            return (false, "Nie udało się zaktualizować wydarzenia");
        }
    }

    public async Task<(bool Success, string? Error)> DeleteEventAsync(int adminId, int eventId)
    {
        try
        {
            var ev = await _context.Events
                .Include(e => e.Tickets)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev == null)
                return (false, "Wydarzenie nie istnieje");

            var title = ev.Title;

            // Usuń bilety powiązane z wydarzeniem (kaskada w bazie również to obsługuje)
            _context.Tickets.RemoveRange(ev.Tickets);

            if (!string.IsNullOrEmpty(ev.ImageUrl))
            {
                var imagePath = Path.Combine(
                    Directory.GetCurrentDirectory(), "wwwroot", ev.ImageUrl.TrimStart('/'));

                if (File.Exists(imagePath))
                    File.Delete(imagePath);
            }

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            await LogActionAsync(adminId, "DeleteEvent", "Event", eventId,
                $"Usunięto wydarzenie: {title}");

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event {EventId}", eventId);
            return (false, "Nie udało się usunąć wydarzenia");
        }
    }

    public async Task<List<AttendeeDto>?> GetEventAttendeesAsync(int adminId, int eventId)
    {
        var ev = await _context.Events
            .AsNoTracking()
            .Include(e => e.Tickets)
                .ThenInclude(t => t.Student)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (ev == null)
            return null;

        return ev.Tickets.Select(t => new AttendeeDto(
            t.Id,
            t.ScanToken,
            t.Student.Email,
            t.Student.FirstName,
            t.Student.LastName,
            t.IsScanned
        )).ToList();
    }

    public async Task<(bool Success, string? Error)> ResetTicketScanAsync(int adminId, Guid scanToken)
    {
        try
        {
            var ticket = await _context.Tickets
                .Include(t => t.Event)
                .FirstOrDefaultAsync(t => t.ScanToken == scanToken);

            if (ticket == null)
                return (false, "Nie znaleziono biletu o podanym kodzie");

            if (!ticket.IsScanned)
                return (false, "Ten bilet nie został jeszcze zeskanowany");

            ticket.IsScanned = false;
            ticket.ScannedAt = null;
            await _context.SaveChangesAsync();

            await LogActionAsync(adminId, "ResetTicket", "Ticket", ticket.Id,
                $"Zresetowano skan biletu #{ticket.Id} (wydarzenie: {ticket.Event.Title})");

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting ticket scan for token {ScanToken}", scanToken);
            return (false, "Nie udało się zresetować biletu");
        }
    }

    public async Task<(bool Success, string? Error)> DeleteTicketAsync(int adminId, int ticketId)
    {
        try
        {
            var ticket = await _context.Tickets
                .Include(t => t.Event)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null)
                return (false, "Nie znaleziono biletu");

            // Usunięcie biletu automatycznie zwalnia miejsce - EnrolledCount liczony jest
            // jako Tickets.Count, więc po usunięciu pojawia się wolne miejsce.
            var eventTitle = ticket.Event.Title;

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();

            await LogActionAsync(adminId, "DeleteTicket", "Ticket", ticketId,
                $"Usunięto bilet #{ticketId} i zwolniono miejsce (wydarzenie: {eventTitle})");

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting ticket {TicketId}", ticketId);
            return (false, "Nie udało się usunąć biletu");
        }
    }

    public async Task<(bool Success, string? Error)> SendTokenEmailAsync(
        int adminId, string token, string email)
    {
        try
        {
            await _emailService.SendOrganizationTokenEmailAsync(email, token);

            await LogActionAsync(adminId, "SendTokenEmail", "OrganizationToken", null,
                $"Wysłano token organizacji na adres: {email}");

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending organization token email to {Email}", email);
            return (false, "Nie udało się wysłać wiadomości e-mail");
        }
    }

    public async Task<List<AuditLogDto>> GetLogsAsync(int adminId, int limit = 100)
    {
        var logs = await _context.AuditLogs
            .AsNoTracking()
            .Include(al => al.User)
            .OrderByDescending(al => al.CreatedAt)
            .Take(limit)
            .Select(al => new AuditLogDto(
                al.Id,
                al.Action,
                al.EntityType,
                al.EntityId,
                al.Details,
                al.CreatedAt,
                al.User.Email
            ))
            .ToListAsync();

        // Logujemy przeglądanie logów (meta!)
        await LogActionAsync(adminId, "ViewLogs", "AuditLog", null,
            $"Przeglądanie logów systemowych (liczba: {logs.Count})");

        return logs;
    }

    private async Task LogActionAsync(int userId, string action, string entityType,
        int? entityId, string? details)
    {
        try
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Nie przerywamy głównej operacji jeśli logowanie się nie powiedzie
            _logger.LogError(ex, "Failed to create audit log entry");
        }
    }
}