namespace EventSystem.Core.Entities;

public class Ticket
{
    public int Id { get; set; }
    public string QrCodeContent { get; set; } = string.Empty;
    public bool IsScanned { get; set; } = false;
    public DateTime? ScannedAt { get; set; }
    
    // DODAJ TO POLE:
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
}