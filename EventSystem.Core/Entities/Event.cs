namespace EventSystem.Core.Entities;

public class Event
{
    // identyfikator (klucz główny)
    public int Id { get; set; }

    // tytuł wydarzenia
    public string Title { get; set; } = string.Empty;

    // opis wydarzenia
    public string Description { get; set; } = string.Empty;

    // data wydarzenia
    public DateTime Date { get; set; }
    
    // miejsce wydarzenia
    public string Location { get; set; } = string.Empty;
    
    // maksymalna ilość uczestników
    public int MaxCapacity { get; set; }
    
    // zdjęcie główne wydarzenia
    public string? ImageUrl { get; set; }

    // id organiztora (klucz obcy)
    public int OrganizerId { get; set; }
    public User Organizer { get; set; } = null!;

    // lista przypisanych wejściówek
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}