using EventSystem.API.DTOs;
using EventSystem.Core.Data;
using EventSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventSystem.API.Services;

public class TicketService
{
    private readonly AppDbContext _context;

    public TicketService(AppDbContext context) => _context = context;

    public async Task<(TicketDto? Ticket, string? Error)> EnrollInEventAsync(int eventId, int studentId)
    {
        var ev = await _context.Events
            .Include(e => e.Tickets)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (ev == null)
            return (null, "Podane wydarzenie nie istnieje");
        if (ev.Date < DateTime.UtcNow)
            return (null, "To wydarzenie już się odbyło");
        if (ev.Tickets.Count >= ev.MaxCapacity)
            return (null, "Brak wolnych miejsc na to wydarzenie");
        if (ev.Tickets.Any(t => t.StudentId == studentId))
            return (null, "Masz już bilet na to wydarzenie");

        var scanToken = Guid.NewGuid();

        var ticket = new Ticket
        {
            EventId = eventId,
            StudentId = studentId,
            ScanToken = scanToken,
            QrCodeContent = scanToken.ToString()
        };

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        return (new TicketDto(
            ticket.Id,
            ev.Title,
            ev.Date,
            ev.Location,
            ticket.QrCodeContent,
            ticket.IsScanned,
            ticket.StudentId), null);
    }

    public async Task<List<TicketDto>> GetMyTicketsAsync(int studentId)
    {
        return await _context.Tickets
            .AsNoTracking()
            .Include(t => t.Event)
            .Where(t => t.StudentId == studentId)
            .Select(t => new TicketDto(
                t.Id,
                t.Event.Title,
                t.Event.Date,
                t.Event.Location,
                t.QrCodeContent,
                t.IsScanned,
                t.StudentId))
            .ToListAsync();
    }

    public async Task<(ScanResultDto? Result, string? Error)> ScanTicketAsync(
        Guid scanToken, int organizerId)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Event)
            .Include(t => t.Student)
            .FirstOrDefaultAsync(t => t.ScanToken == scanToken);

        if (ticket == null)
            return (null, "Nie znaleziono biletu o podanym kodzie");
        if (ticket.Event.OrganizerId != organizerId)
            return (null, "Nie masz uprawnień do weryfikacji biletów na to wydarzenie");
        if (ticket.IsScanned)
            return (null, $"Ten bilet został już zeskanowany o godzinie {ticket.ScannedAt:HH:mm:ss}");

        ticket.IsScanned = true;
        ticket.ScannedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (new ScanResultDto(
            ticket.Id,
            ticket.Student.FirstName,
            ticket.Student.LastName,
            ticket.Event.Title,
            ticket.ScannedAt!.Value), null);
    }
}