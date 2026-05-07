using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace EventSystem.Core.Entities;

public class Event
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Location { get; set; } = string.Empty;

    public int OrganizerId { get; set; }
    public User Organizer { get; set; } = null!;

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
