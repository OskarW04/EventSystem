using EventSystem.API.Services;
using EventSystem.Core.Data;
using Microsoft.EntityFrameworkCore;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("EventSystem.Tests")]

namespace EventSystem.API.Infrastructure;

// #4 - zadanie w tle. Cyklicznie sprawdza pre-savy wydarzeń, których
// RegistrationOpensAt już minął (a samo wydarzenie się jeszcze nie odbyło),
// i wysyła do nich maila "rejestracja otwarta". NotifiedAt stemplowany jest
// dopiero po udanej wysyłce, więc nieudane maile są ponawiane w kolejnym cyklu.
public class RegistrationOpeningNotifier : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
    private const int BatchSize = 200;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RegistrationOpeningNotifier> _logger;

    public RegistrationOpeningNotifier(
        IServiceScopeFactory scopeFactory,
        ILogger<RegistrationOpeningNotifier> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        do
        {
            try
            {
                await NotifyOpenedRegistrationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas wysyłki powiadomień o otwarciu rejestracji");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task NotifyOpenedRegistrationsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        await NotifyOpenedRegistrationsAsync(db, emailService, _logger, DateTime.UtcNow, BatchSize, ct);
    }

    // Rdzeń logiki wydzielony tak, by dało się go przetestować bez timera i DI.
    // Zwraca liczbę wysłanych powiadomień. Wyjątek przy jednym adresie nie
    // blokuje pozostałych - logujemy i lecimy dalej (NotifiedAt zostaje null).
    internal static async Task<int> NotifyOpenedRegistrationsAsync(
        AppDbContext db,
        IEmailService emailService,
        ILogger logger,
        DateTime now,
        int batchSize,
        CancellationToken ct)
    {
        var pending = await db.EventPresaves
            .Include(p => p.Event)
            .Include(p => p.Student)
            .Where(p => p.NotifiedAt == null
                && p.Event.RegistrationOpensAt != null
                && p.Event.RegistrationOpensAt <= now
                && p.Event.Date > now)
            .OrderBy(p => p.Id)
            .Take(batchSize)
            .ToListAsync(ct);

        if (pending.Count == 0)
            return 0;

        var sent = 0;

        foreach (var presave in pending)
        {
            try
            {
                await emailService.SendRegistrationOpenEmailAsync(
                    presave.Student.Email, presave.EventId, presave.Event.Title);

                presave.NotifiedAt = now;
                sent++;
            }
            catch (Exception ex)
            {
                // Zostawiamy NotifiedAt = null - ponowimy w kolejnym cyklu.
                logger.LogError(ex,
                    "Nie udało się wysłać powiadomienia o otwarciu rejestracji do {Email} (wydarzenie {EventId})",
                    presave.Student.Email, presave.EventId);
            }
        }

        if (sent > 0)
            await db.SaveChangesAsync(ct);

        return sent;
    }
}
