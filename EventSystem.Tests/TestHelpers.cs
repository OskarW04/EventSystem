using EventSystem.API.Services;
using EventSystem.Core.Data;
using EventSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventSystem.Tests;

// Fabryka kontekstu na bazie in-memory + drobne helpery do budowania danych.
// Każdy test dostaje izolowaną bazę (unikalna nazwa), więc testy nie kolidują.
internal static class TestHelpers
{
    public static AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    public static User AddUser(this AppDbContext db, int id, string email)
    {
        var user = new User
        {
            Id = id,
            FirstName = "Test",
            LastName = "User",
            Email = email,
            PasswordHash = "x",
            RoleId = 1
        };
        db.Users.Add(user);
        return user;
    }

    // Wydarzenie z konfigurowalnym oknem presave/rejestracji. Domyślnie:
    // odbywa się w przyszłości, rejestracja otwiera się za godzinę, presave od razu.
    public static Event AddEvent(
        this AppDbContext db,
        int id,
        int organizerId,
        DateTime? date = null,
        DateTime? registrationOpensAt = null,
        DateTime? presaveOpensAt = null)
    {
        var now = DateTime.UtcNow;
        var ev = new Event
        {
            Id = id,
            Title = $"Event {id}",
            Description = "Opis wydarzenia testowego",
            Date = date ?? now.AddDays(7),
            Location = "Warszawa",
            MaxCapacity = 100,
            OrganizerId = organizerId,
            RegistrationOpensAt = registrationOpensAt ?? now.AddHours(1),
            PresaveOpensAt = presaveOpensAt
        };
        db.Events.Add(ev);
        return ev;
    }
}

// Zliczający fake serwisu mailowego - rejestruje każde wywołanie wysyłki.
internal sealed class FakeEmailService : IEmailService
{
    public List<(string To, int EventId, string Title)> Sent { get; } = new();

    // Gdy ustawione, wysyłka na ten adres rzuca wyjątkiem (symulacja złego adresu).
    public string? ThrowForEmail { get; set; }

    public Task SendPasswordResetEmailAsync(string to, string resetToken) => Task.CompletedTask;

    public Task SendOrganizationTokenEmailAsync(string to, string token) => Task.CompletedTask;

    public Task SendRegistrationOpenEmailAsync(string to, int eventId, string eventTitle)
    {
        if (ThrowForEmail != null && to == ThrowForEmail)
            throw new InvalidOperationException($"Niepoprawny adres: {to}");

        Sent.Add((to, eventId, eventTitle));
        return Task.CompletedTask;
    }
}
