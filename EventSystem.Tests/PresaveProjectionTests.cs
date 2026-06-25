using EventSystem.API.Services;
using EventSystem.Core.Entities;
using Xunit;

namespace EventSystem.Tests;

// #4 - HasPresaved / PresaveCount wypełniane w projekcjach DTO bez N+1.
public class PresaveProjectionTests
{
    private const int OrganizerId = 1;
    private const int StudentA = 2;
    private const int StudentB = 3;

    private static EventService Seed(out EventSystem.Core.Data.AppDbContext db)
    {
        db = TestHelpers.NewContext();
        db.AddUser(OrganizerId, "org@test.pl");
        db.AddUser(StudentA, "a@test.pl");
        db.AddUser(StudentB, "b@test.pl");
        db.AddEvent(10, OrganizerId); // w przyszłości, rejestracja za godzinę

        // Dwa presavy: student A i B.
        db.EventPresaves.Add(new EventPresave { EventId = 10, StudentId = StudentA, CreatedAt = DateTime.UtcNow });
        db.EventPresaves.Add(new EventPresave { EventId = 10, StudentId = StudentB, CreatedAt = DateTime.UtcNow });
        db.SaveChanges();
        return new EventService(db);
    }

    [Fact]
    public async Task UpcomingEvents_HasPresaved_TrueForPresavedStudent()
    {
        var service = Seed(out _);

        var forA = await service.GetUpcomingEventsAsync(StudentA);
        Assert.True(forA.Single().HasPresaved);
    }

    [Fact]
    public async Task UpcomingEvents_HasPresaved_FalseForOtherStudent()
    {
        var service = Seed(out _);

        var forOther = await service.GetUpcomingEventsAsync(999);
        Assert.False(forOther.Single().HasPresaved);
    }

    [Fact]
    public async Task UpcomingEvents_HasPresaved_FalseForAnonymous()
    {
        var service = Seed(out _);

        var anon = await service.GetUpcomingEventsAsync(null);
        Assert.False(anon.Single().HasPresaved);
    }

    [Fact]
    public async Task EventDetails_HasPresaved_ReflectsStudent()
    {
        var service = Seed(out _);

        var detailsA = await service.GetEventDetailsAsync(10, StudentA);
        var detailsAnon = await service.GetEventDetailsAsync(10, null);

        Assert.True(detailsA!.HasPresaved);
        Assert.False(detailsAnon!.HasPresaved);
    }

    [Fact]
    public async Task OrganizerEvents_PresaveCount_CountsAllPresaves()
    {
        var service = Seed(out _);

        var events = await service.GetOrganizerEventsAsync(OrganizerId);
        Assert.Equal(2, events.Single().PresaveCount);
    }
}
