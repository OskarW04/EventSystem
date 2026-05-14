namespace EventSystem.Core.Entities;

public class SocialLink
{
    public int Id { get; set; }
    public string PlatformName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}