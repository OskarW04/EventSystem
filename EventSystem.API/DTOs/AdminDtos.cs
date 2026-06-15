namespace EventSystem.API.DTOs;

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