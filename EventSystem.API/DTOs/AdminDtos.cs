using System.ComponentModel.DataAnnotations;

namespace EventSystem.API.DTOs;

// Tworzenie użytkownika z panelu admina (POST /api/admin/users).
// Hasło ustawia admin; Role to nazwa roli ("Student"/"Organizer").
// Null/puste Role => "Student". Roli "Admin" nie można nadać tą drogą.
public record AdminCreateUserDto(
    [Required, StringLength(100, MinimumLength = 1)] string FirstName,
    [Required, StringLength(100, MinimumLength = 1)] string LastName,
    [Required, EmailAddress, StringLength(200)] string Email,
    [Required, StringLength(100, MinimumLength = 6)] string Password,
    string? Role
);

public record UserListDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    DateTime CreatedAt,
    bool HasActiveEvents
);

public record AuditLogDto(
    int Id,
    string Action,
    string EntityType,
    int? EntityId,
    string? Details,
    DateTime CreatedAt,
    string UserEmail
);

public record UpdateRoleDto(
    string NewRole
);

public record SendTokenEmailDto(
    string Token,
    string Email
);

public record AttendeeDto(
    int Id,
    Guid ScanToken,
    string StudentEmail,
    string FirstName,
    string LastName,
    bool IsScanned
);