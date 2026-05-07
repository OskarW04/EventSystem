using System;
using System.Collections.Generic;
using System.Text;

namespace EventSystem.Core.Entities;

public class OrganizationToken
{
    public int Id { get; set; }
    public string TokenValue { get; set; } = string.Empty;
    public bool IsUsed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;

    public int? UsedById { get; set; }
    public User? UsedBy { get; set; }
}