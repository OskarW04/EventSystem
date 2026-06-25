namespace EventSystem.Core.Entities;

// Zapis na powiadomienie mailowe o starcie rejestracji (#4 — "presave").
// NIE tworzy biletu, nie daje pierwszeństwa ani miejsca — to subskrypcja maila
// "powiadom mnie, gdy ruszy rejestracja". Po nadejściu RegistrationOpensAt
// zadanie w tle wysyła pre-saverom maila i stempluje NotifiedAt.
public class EventPresave
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Moment (UTC) wysłania maila o otwarciu rejestracji. Null = jeszcze nie
    // powiadomiono. Stempel (a nie flaga bool) chroni przed dublem wysyłki.
    public DateTime? NotifiedAt { get; set; }
}
