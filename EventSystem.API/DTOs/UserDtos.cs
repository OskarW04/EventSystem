namespace EventSystem.API.DTOs;

public record SocialLinkDto(string PlatformName, string Url);

public record UpdateProfileDto(string? Bio, List<SocialLinkDto> SocialLinks);

public record PublicProfileDto(
    string FirstName,
    string LastName,
    string? Bio,
    List<SocialLinkDto> SocialLinks
);