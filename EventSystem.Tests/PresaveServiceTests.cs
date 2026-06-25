using EventSystem.API.Services;
using Xunit;

namespace EventSystem.Tests;

// #4 - reguły okna presave (AddPresaveAsync) oraz idempotentny DELETE.
public class PresaveServiceTests
{
    private const int OrganizerId = 1;
    private const int StudentId = 2;

    // Helper: kontekst z organizatorem, studentem i jednym wydarzeniem.
    private static (AppDbContextWrapper Wrapper, EventService Service) Setup(
        DateTime? eventDate = null,
        DateTime? registrationOpensAt = null,
        DateTime? presaveOpensAt = null)
    {
        var db = TestHelpers.NewContext();
        db.AddUser(OrganizerId, "org@test.pl");
        db.AddUser(StudentId, "student@test.pl");
        db.AddEvent(10, OrganizerId, eventDate, registrationOpensAt, presaveOpensAt);
        db.SaveChanges();
        return (new AppDbContextWrapper(db), new EventService(db));
    }

    [Fact]
    public async Task Presave_Succeeds_WhenRegistrationNotYetOpen_AndPresaveWindowOpen()
    {
        var now = DateTime.UtcNow;
        var (wrap, service) = Setup(
            eventDate: now.AddDays(7),
            registrationOpensAt: now.AddHours(1),
            presaveOpensAt: now.AddHours(-1)); // okno presave już otwarte

        var (outcome, error) = await service.AddPresaveAsync(10, StudentId);

        Assert.Equal(PresaveOutcome.Created, outcome);
        Assert.Null(error);
        Assert.Equal(1, wrap.Db.EventPresaves.Count());
    }

    [Fact]
    public async Task Presave_Succeeds_WhenPresaveOpensAtNull()
    {
        var now = DateTime.UtcNow;
        var (_, service) = Setup(
            registrationOpensAt: now.AddHours(1),
            presaveOpensAt: null); // null = presave dostępny od razu

        var (outcome, _) = await service.AddPresaveAsync(10, StudentId);

        Assert.Equal(PresaveOutcome.Created, outcome);
    }

    [Fact]
    public async Task Presave_Rejected_WhenRegistrationAlreadyOpen()
    {
        var now = DateTime.UtcNow;
        var (_, service) = Setup(registrationOpensAt: now.AddHours(-1)); // już otwarta

        var (outcome, error) = await service.AddPresaveAsync(10, StudentId);

        Assert.Equal(PresaveOutcome.Invalid, outcome);
        Assert.Equal("Rejestracja jest już otwarta — odbierz bilet", error);
    }

    [Fact]
    public async Task Presave_Rejected_WhenRegistrationOpensAtNull()
    {
        // null = rejestracja otwarta od razu -> presave nie ma sensu.
        // Helper koalescuje null do wartości domyślnej, więc zerujemy ręcznie.
        var (wrap, service) = Setup();
        var ev = wrap.Db.Events.Single();
        ev.RegistrationOpensAt = null;
        wrap.Db.SaveChanges();

        var (outcome, error) = await service.AddPresaveAsync(10, StudentId);

        Assert.Equal(PresaveOutcome.Invalid, outcome);
        Assert.Equal("Rejestracja jest już otwarta — odbierz bilet", error);
    }

    [Fact]
    public async Task Presave_Rejected_WhenPresaveWindowNotStarted()
    {
        var now = DateTime.UtcNow;
        var (_, service) = Setup(
            registrationOpensAt: now.AddHours(2),
            presaveOpensAt: now.AddHours(1)); // presave dopiero za godzinę

        var (outcome, error) = await service.AddPresaveAsync(10, StudentId);

        Assert.Equal(PresaveOutcome.Invalid, outcome);
        Assert.Equal("Pre-rejestracja jeszcze się nie rozpoczęła", error);
    }

    [Fact]
    public async Task Presave_Rejected_WhenEventInPast()
    {
        var now = DateTime.UtcNow;
        var (_, service) = Setup(
            eventDate: now.AddDays(-1),
            registrationOpensAt: now.AddDays(-2)); // i tak past, ale Date sprawdzane

        var (outcome, error) = await service.AddPresaveAsync(10, StudentId);

        Assert.Equal(PresaveOutcome.Invalid, outcome);
        Assert.Equal("To wydarzenie już się odbyło", error);
    }

    [Fact]
    public async Task Presave_Rejected_WhenStudentIsOrganizer()
    {
        var (_, service) = Setup();

        var (outcome, error) = await service.AddPresaveAsync(10, OrganizerId);

        Assert.Equal(PresaveOutcome.Invalid, outcome);
        Assert.Equal("Nie możesz zapisać się na własne wydarzenie", error);
    }

    [Fact]
    public async Task Presave_Rejected_WhenEventDoesNotExist()
    {
        var (_, service) = Setup();

        var (outcome, error) = await service.AddPresaveAsync(999, StudentId);

        Assert.Equal(PresaveOutcome.Invalid, outcome);
        Assert.Equal("Podane wydarzenie nie istnieje", error);
    }

    [Fact]
    public async Task Presave_Duplicate_ReturnsAlreadyPresaved_AndDoesNotAddSecondRow()
    {
        var (wrap, service) = Setup(presaveOpensAt: DateTime.UtcNow.AddHours(-1));

        var first = await service.AddPresaveAsync(10, StudentId);
        var second = await service.AddPresaveAsync(10, StudentId);

        Assert.Equal(PresaveOutcome.Created, first.Outcome);
        Assert.Equal(PresaveOutcome.AlreadyPresaved, second.Outcome);
        Assert.Equal(1, wrap.Db.EventPresaves.Count());
    }

    [Fact]
    public async Task RemovePresave_IsIdempotent()
    {
        var (wrap, service) = Setup(presaveOpensAt: DateTime.UtcNow.AddHours(-1));
        await service.AddPresaveAsync(10, StudentId);
        Assert.Equal(1, wrap.Db.EventPresaves.Count());

        // Pierwsze usunięcie kasuje wiersz, drugie nie rzuca i nic nie zmienia.
        await service.RemovePresaveAsync(10, StudentId);
        await service.RemovePresaveAsync(10, StudentId);

        Assert.Equal(0, wrap.Db.EventPresaves.Count());
    }
}

// Cienki uchwyt na kontekst, żeby testy mogły odpytywać stan po operacji
// (EventService trzyma własny kontekst; tu dzielimy tę samą instancję).
internal sealed class AppDbContextWrapper
{
    public EventSystem.Core.Data.AppDbContext Db { get; }
    public AppDbContextWrapper(EventSystem.Core.Data.AppDbContext db) => Db = db;
}
