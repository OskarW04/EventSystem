namespace EventSystem.Core.Entities;

// Pojedyncza odsłona wydarzenia (#6/#5 — licznik kliknięć z ostatnich 24h).
// Zapisywana fire-and-forget z POST /events/{id}/view. clicks24h liczone jest
// jako liczba wierszy z CreatedAt z ostatnich 24h.
public class EventView
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Klucz klienta (hash IP) do deduplikacji odsłon w krótkim oknie czasowym.
    // Null gdy adresu nie udało się ustalić - wtedy odsłona liczona jest zawsze.
    public string? ClientKey { get; set; }
}
