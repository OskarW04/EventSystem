namespace EventSystem.API.DTOs;

public record SocialLinkDto(string PlatformName, string Url);

public record UpdateProfileDto(string? Bio, List<SocialLinkDto> SocialLinks);

public record PublicProfileDto(
    string FirstName,
    string LastName,
    string? Bio,
    List<SocialLinkDto> SocialLinks
);

public record UserProfileDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string? Bio,
    string Role,
    DateTime CreatedAt,
    List<SocialLinkDto> SocialLinks
);

public record ChangePasswordDto(
    string CurrentPassword,
    string NewPassword
);

public record UpdateUserDetailsDto(
    string FirstName,
    string LastName,
    string Email
);

public record DeleteAccountDto(
    string Password
);
