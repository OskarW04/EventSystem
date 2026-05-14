using System;

namespace EventSystem.Core.Entities;

public class Feedback
{
    public int Id { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
}