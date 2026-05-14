using System.Collections.Generic;

namespace EventSystem.Core.Entities;

public class Speaker
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string? LinkedInUrl { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public ICollection<AgendaItem> AgendaItems { get; set; } = new List<AgendaItem>();
}