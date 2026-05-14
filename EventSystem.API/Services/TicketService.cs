using EventSystem.API.DTOs;
using EventSystem.Core.Data;
using EventSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventSystem.API.Services;

public class TicketService
{
    private readonly AppDbContext _context;
    public TicketService(AppDbContext context) => _context = context;

    public async Task<string> EnrollInEventAsync(int eventId, int studentId)
    {
        var ev = await _context.Events.Include(e => e.Tickets).FirstOrDefaultAsync(e => e.Id == eventId);
        if (ev == null) return "Wydarzenie nie istnieje.";
        if (ev.Date < DateTime.UtcNow) return "Wydarzenie już się odbyło.";
        if (ev.Tickets.Count >= ev.MaxCapacity) return "Brak wolnych miejsc.";
        if (ev.Tickets.Any(t => t.StudentId == studentId)) return "Masz już bilet na to wydarzenie.";

        var ticket = new Ticket
        {
            EventId = eventId,
            StudentId = studentId,
            QrCodeContent = $"http://localhost:3000/public/profile/{studentId}" // URL na front, który odczyta bio
        };
        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();
        return "Success";
    }

    public async Task<List<TicketDto>> GetMyTicketsAsync(int studentId)
    {
        return await _context.Tickets
            .Include(t => t.Event)
            .Where(t => t.StudentId == studentId)
            .Select(t => new TicketDto(t.Id, t.Event.Title, t.Event.Date, t.Event.Location, t.QrCodeContent, t.IsScanned))
            .ToListAsync();
    }

    public async Task<string> ScanTicketAsync(int ticketId, int organizerId)
    {
        var ticket = await _context.Tickets.Include(t => t.Event).FirstOrDefaultAsync(t => t.Id == ticketId);
        if (ticket == null) return "Bilet nie istnieje.";
        if (ticket.Event.OrganizerId != organizerId) return "Brak uprawnień do tego wydarzenia.";
        if (ticket.IsScanned) return "Bilet został już zeskanowany.";

        ticket.IsScanned = true;
        ticket.ScannedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return "Success";
    }
}