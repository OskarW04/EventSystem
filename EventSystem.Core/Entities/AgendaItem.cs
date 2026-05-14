using System;
using System.Collections.Generic;

namespace EventSystem.Core.Entities;

public class AgendaItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public ICollection<Speaker> Speakers { get; set; } = new List<Speaker>();
}