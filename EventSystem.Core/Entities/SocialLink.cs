namespace EventSystem.Core.Entities;

public class SocialLink
{
    public int Id { get; set; }

    // nazwa platformy
    public string PlatformName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    // przypisanie do użytkownika
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}