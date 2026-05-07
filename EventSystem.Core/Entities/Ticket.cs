using System;
using System.Collections.Generic;
using System.Text;

namespace EventSystem.Core.Entities;

public class Ticket
{
    public int Id { get; set; }
    public string QrCodeContent { get; set; } = string.Empty;
    public bool IsScanned { get; set; } = false;
    public DateTime? ScannedAt { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
}
