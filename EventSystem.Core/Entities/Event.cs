namespace EventSystem.Core.Entities;

public class Event
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Data/godzina rozpoczęcia. Zachowana jako "Date" dla kompatybilności wstecznej.
    public DateTime Date { get; set; }

    // Data/godzina zakończenia. Opcjonalna - starsze wydarzenia mogą jej nie mieć.
    public DateTime? EndDate { get; set; }

    public string Location { get; set; } = string.Empty;

    // Przyjazna nazwa miejsca (np. "Aula Główna PW") pokazywana obok adresu/mapy.
    // Opcjonalna - starsze wydarzenia mogą jej nie mieć.
    public string? LocationName { get; set; }

    // Współrzędne geograficzne (OpenStreetMap / Leaflet). Opcjonalne.
    public double? Lat { get; set; }
    public double? Lng { get; set; }

    public int MaxCapacity { get; set; }
    public string? ImageUrl { get; set; }

    public int OrganizerId { get; set; }
    public User Organizer { get; set; } = null!;

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}