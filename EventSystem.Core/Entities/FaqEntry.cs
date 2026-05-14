namespace EventSystem.Core.Entities;

public class FaqEntry
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
}