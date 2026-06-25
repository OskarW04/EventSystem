using EventSystem.API.DTOs;
using EventSystem.Core.Data;
using EventSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventSystem.API.Services;

public class TicketService
{
    private readonly AppDbContext _context;

    public TicketService(AppDbContext context) => _context = context;

    // Sentinel zwracany, gdy rejestracja jeszcze się nie otworzyła (#4).
    // Kontroler tłumaczy go na 409 z ciałem { error, opensAt }.
    public const string RegistrationNotOpenError = "registration_not_open";

    public async Task<(TicketDto? Ticket, string? Error, DateTime? RegistrationOpensAt)>
        EnrollInEventAsync(int eventId, int studentId)
    {
        var ev = await _context.Events
            .Include(e => e.Tickets)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (ev == null)
            return (null, "Podane wydarzenie nie istnieje", null);
        // Organizator nie może zapisać się na własne wydarzenie - nawet jeśli
        // jego rola została w międzyczasie zmieniona na Student.
        if (ev.OrganizerId == studentId)
            return (null, "Nie możesz zapisać się na własne wydarzenie", null);
        // #4 - twarda blokada zapisu przed otwarciem rejestracji.
        if (ev.RegistrationOpensAt.HasValue && DateTime.UtcNow < ev.RegistrationOpensAt.Value)
            return (null, RegistrationNotOpenError, ev.RegistrationOpensAt);
        if (ev.Date < DateTime.UtcNow)
            return (null, "To wydarzenie już się odbyło", null);
        if (ev.Tickets.Count >= ev.MaxCapacity)
            return (null, "Brak wolnych miejsc na to wydarzenie", null);
        if (ev.Tickets.Any(t => t.StudentId == studentId))
            return (null, "Masz już bilet na to wydarzenie", null);

        var scanToken = Guid.NewGuid();

        var ticket = new Ticket
        {
            EventId = eventId,
            StudentId = studentId,
            ScanToken = scanToken,
            QrCodeContent = scanToken.ToString()
        };

        _context.Tickets.Add(ticket);

        // #4 - sprzątanie: po właściwym zapisie presave (zapis na powiadomienie)
        // jest już bezużyteczny - usuwamy go, żeby nie zaśmiecał liczników.
        var presave = await _context.EventPresaves
            .FirstOrDefaultAsync(p => p.EventId == eventId && p.StudentId == studentId);
        if (presave != null)
            _context.EventPresaves.Remove(presave);

        await _context.SaveChangesAsync();

        return (new TicketDto(
            ticket.Id,
            ev.Id,
            ev.Title,
            ev.Date,
            ev.Date,
            ev.EndDate,
            ev.Location,
            ticket.QrCodeContent,
            ticket.IsScanned,
            ticket.StudentId,
            (ev.EndDate ?? ev.Date) < DateTime.UtcNow), null, null);
    }

    public async Task<List<TicketDto>> GetMyTicketsAsync(int studentId)
    {
        // Zmienna lokalna (nie DateTime.UtcNow w drzewie wyrażeń) — EF
        // sparametryzuje ją w zapytaniu zamiast tłumaczyć wywołanie.
        var now = DateTime.UtcNow;
        return await _context.Tickets
            .AsNoTracking()
            .Include(t => t.Event)
            .Where(t => t.StudentId == studentId)
            .Select(t => new TicketDto(
                t.Id,
                t.EventId,
                t.Event.Title,
                t.Event.Date,
                t.Event.Date,
                t.Event.EndDate,
                t.Event.Location,
                t.QrCodeContent,
                t.IsScanned,
                t.StudentId,
                (t.Event.EndDate ?? t.Event.Date) < now))
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
        // Po dacie zakończenia (a gdy jej brak — po dacie startu) bilet wygasa
        // i nie da się go już zweryfikować przy wejściu.
        if (DateTime.UtcNow > (ticket.Event.EndDate ?? ticket.Event.Date))
            return (null, "To wydarzenie już się zakończyło — bilet wygasł");
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