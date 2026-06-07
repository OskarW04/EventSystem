namespace EventSystem.Core.Entities;

public class Ticket
{
    // id biletu (klucz główny)
    public int Id { get; set; }
    public string QrCodeContent { get; set; } = string.Empty;

    // metryki do walidacji czasowej wejściówek
    public bool IsScanned { get; set; } = false;
    public DateTime? ScannedAt { get; set; }

    // dwustronny klucz obcy N-1
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    // 1-1
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
}