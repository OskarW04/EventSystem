namespace EventSystem.API.DTOs;

public record LoginDto(string Email, string Password);

public record RegisterStudentDto(
    string FirstName,
    string LastName,
    string Email,
    string Password
);

public record RegisterOrganizerDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Token
);

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    string Role,
    int UserId
);

public record ForgotPasswordDto(string Email);

public record ResetPasswordDto(
    string Email,
    string ResetToken,
    string NewPassword
);
