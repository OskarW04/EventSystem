namespace EventSystem.Core.Entities;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public ICollection<SocialLink> SocialLinks { get; set; } = new List<SocialLink>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<EventPresave> Presaves { get; set; } = new List<EventPresave>();
    public ICollection<Event> CreatedEvents { get; set; } = new List<Event>();
    public ICollection<OrganizationToken> CreatedTokens { get; set; } = new List<OrganizationToken>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
}