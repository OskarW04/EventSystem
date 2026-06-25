using EventSystem.API.Infrastructure;
using EventSystem.Core.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EventSystem.Tests;

// #4 - zadanie w tle: wysyła maila o otwarciu rejestracji dokładnie raz
// i stempluje NotifiedAt; respektuje warunki okna i nie blokuje się złym adresem.
public class RegistrationOpeningNotifierTests
{
    private const int OrganizerId = 1;
    private const int StudentId = 2;
    private const int BatchSize = 200;

    [Fact]
    public async Task Notifier_SendsEmail_AndStampsNotifiedAt_ExactlyOnce()
    {
        var db = TestHelpers.NewContext();
        var now = DateTime.UtcNow;
        db.AddUser(OrganizerId, "org@test.pl");
        var student = db.AddUser(StudentId, "student@test.pl");
        // Rejestracja już otwarta, wydarzenie wciąż w przyszłości.
        db.AddEvent(10, OrganizerId, date: now.AddDays(3), registrationOpensAt: now.AddMinutes(-5));
        db.EventPresaves.Add(new EventPresave { EventId = 10, StudentId = StudentId, CreatedAt = now });
        db.SaveChanges();

        var email = new FakeEmailService();

        var first = await RegistrationOpeningNotifier.NotifyOpenedRegistrationsAsync(
            db, email, NullLogger.Instance, now, BatchSize, CancellationToken.None);
        // Drugi przebieg nie powinien już nic wysłać (NotifiedAt chroni przed dublem).
        var second = await RegistrationOpeningNotifier.NotifyOpenedRegistrationsAsync(
            db, email, NullLogger.Instance, now.AddMinutes(1), BatchSize, CancellationToken.None);

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        Assert.Single(email.Sent);
        Assert.Equal(student.Email, email.Sent[0].To);

        var presave = db.EventPresaves.Single();
        Assert.NotNull(presave.NotifiedAt);
    }

    [Fact]
    public async Task Notifier_Skips_WhenRegistrationNotYetOpen()
    {
        var db = TestHelpers.NewContext();
        var now = DateTime.UtcNow;
        db.AddUser(OrganizerId, "org@test.pl");
        db.AddUser(StudentId, "student@test.pl");
        db.AddEvent(10, OrganizerId, date: now.AddDays(3), registrationOpensAt: now.AddHours(1));
        db.EventPresaves.Add(new EventPresave { EventId = 10, StudentId = StudentId, CreatedAt = now });
        db.SaveChanges();

        var email = new FakeEmailService();
        var sent = await RegistrationOpeningNotifier.NotifyOpenedRegistrationsAsync(
            db, email, NullLogger.Instance, now, BatchSize, CancellationToken.None);

        Assert.Equal(0, sent);
        Assert.Empty(email.Sent);
        Assert.Null(db.EventPresaves.Single().NotifiedAt);
    }

    [Fact]
    public async Task Notifier_Skips_WhenEventAlreadyPassed()
    {
        var db = TestHelpers.NewContext();
        var now = DateTime.UtcNow;
        db.AddUser(OrganizerId, "org@test.pl");
        db.AddUser(StudentId, "student@test.pl");
        // Rejestracja się otworzyła, ale wydarzenie już za nami.
        db.AddEvent(10, OrganizerId, date: now.AddHours(-1), registrationOpensAt: now.AddHours(-3));
        db.EventPresaves.Add(new EventPresave { EventId = 10, StudentId = StudentId, CreatedAt = now });
        db.SaveChanges();

        var email = new FakeEmailService();
        var sent = await RegistrationOpeningNotifier.NotifyOpenedRegistrationsAsync(
            db, email, NullLogger.Instance, now, BatchSize, CancellationToken.None);

        Assert.Equal(0, sent);
        Assert.Empty(email.Sent);
    }

    [Fact]
    public async Task Notifier_BadAddress_DoesNotBlockOthers_AndRetriesNextCycle()
    {
        var db = TestHelpers.NewContext();
        var now = DateTime.UtcNow;
        db.AddUser(OrganizerId, "org@test.pl");
        db.AddUser(StudentId, "bad@test.pl");
        db.AddUser(3, "good@test.pl");
        db.AddEvent(10, OrganizerId, date: now.AddDays(3), registrationOpensAt: now.AddMinutes(-5));
        db.EventPresaves.Add(new EventPresave { EventId = 10, StudentId = StudentId, CreatedAt = now });
        db.EventPresaves.Add(new EventPresave { EventId = 10, StudentId = 3, CreatedAt = now });
        db.SaveChanges();

        var email = new FakeEmailService { ThrowForEmail = "bad@test.pl" };

        // Pierwszy cykl: zły adres rzuca, dobry przechodzi.
        var first = await RegistrationOpeningNotifier.NotifyOpenedRegistrationsAsync(
            db, email, NullLogger.Instance, now, BatchSize, CancellationToken.None);

        Assert.Equal(1, first);
        Assert.Single(email.Sent);
        Assert.Equal("good@test.pl", email.Sent[0].To);
        // Dobry ma NotifiedAt, zły wciąż null -> będzie ponowiony.
        Assert.Equal(1, db.EventPresaves.Count(p => p.NotifiedAt != null));
        Assert.Equal(1, db.EventPresaves.Count(p => p.NotifiedAt == null));

        // Drugi cykl: adres już naprawiony, zły presave dochodzi.
        email.ThrowForEmail = null;
        var second = await RegistrationOpeningNotifier.NotifyOpenedRegistrationsAsync(
            db, email, NullLogger.Instance, now.AddMinutes(1), BatchSize, CancellationToken.None);

        Assert.Equal(1, second);
        Assert.Equal(0, db.EventPresaves.Count(p => p.NotifiedAt == null));
    }
}
