namespace EventSystem.Core.Entities;

public class AuditLog
{
      public int Id { get; set; }
      public string Action { get; set; } = string.Empty;
      public string EntityType { get; set; } = string.Empty;
      public int? EntityId { get; set; }
      public string? Details { get; set; }
      public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

      public int UserId { get; set; }
      public User User { get; set; } = null!;
}