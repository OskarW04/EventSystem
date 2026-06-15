using EventSystem.API.Services;
using EventSystem.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace EventSystem.API.Infrastructure;

// #4 - zadanie w tle. Cyklicznie sprawdza pre-savy wydarzeń, których
// RegistrationOpensAt już minął, i wysyła do nich maila "rejestracja otwarta".
// Każdy pre-save oznaczany jest jako Notified dopiero po udanej wysyłce, więc
// nieudane maile są ponawiane w kolejnym cyklu.
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

        var now = DateTime.UtcNow;

        var pending = await db.EventPresaves
            .Include(p => p.Event)
            .Include(p => p.Student)
            .Where(p => !p.Notified
                && p.Event.RegistrationOpensAt != null
                && p.Event.RegistrationOpensAt <= now)
            .OrderBy(p => p.Id)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (pending.Count == 0)
            return;

        var sentAny = false;

        foreach (var presave in pending)
        {
            try
            {
                await emailService.SendRegistrationOpenEmailAsync(
                    presave.Student.Email, presave.EventId, presave.Event.Title);

                presave.Notified = true;
                sentAny = true;
            }
            catch (Exception ex)
            {
                // Zostawiamy Notified = false - ponowimy w kolejnym cyklu.
                _logger.LogError(ex,
                    "Nie udało się wysłać powiadomienia o otwarciu rejestracji do {Email} (wydarzenie {EventId})",
                    presave.Student.Email, presave.EventId);
            }
        }

        if (sentAny)
            await db.SaveChangesAsync(ct);
    }
}
