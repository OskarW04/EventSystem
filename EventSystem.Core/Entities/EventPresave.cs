namespace EventSystem.Core.Entities;

// Zgłoszenie chęci zapisu na wydarzenie, którego rejestracja jeszcze się nie
// otworzyła (#4 — pre-save). Po nadejściu RegistrationOpensAt zadanie w tle
// wysyła pre-saverom maila "rejestracja otwarta" i ustawia Notified = true.
public class EventPresave
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Czy wysłano już do tego studenta maila o otwarciu rejestracji.
    public bool Notified { get; set; }
}
