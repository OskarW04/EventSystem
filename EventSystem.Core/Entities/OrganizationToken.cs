namespace EventSystem.Core.Entities;

public class OrganizationToken
{
    // id tokenu (klucz główny)
    public int Id { get; set; }
    public string TokenValue { get; set; } = string.Empty;

    // reset po założeniu wydarzenia
    public bool IsUsed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;

    // TODO: wyjaśnić czy połączenie do usera-organizatora
    public int? UsedById { get; set; }
    public User? UsedBy { get; set; }
}