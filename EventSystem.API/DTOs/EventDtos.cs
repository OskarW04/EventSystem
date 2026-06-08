namespace EventSystem.API.DTOs;

public record CreateEventDto(
    string Title,
    string Description,
    DateTime Date,
    string Location,
    int MaxCapacity
);

public record UpdateEventDto(
    string Title,
    string Description,
    DateTime Date,
    string Location,
    int MaxCapacity
);


public record EventDto(
    int Id,
    string Title,
    string Description,
    DateTime Date,
    string Location,
    int MaxCapacity,
    string? ImageUrl,
    int EnrolledCount
);

public record EventDetailsDto(
    int Id,
    string Title,
    string Description,
    DateTime Date,
    string Location,
    int MaxCapacity,
    string? ImageUrl,
    int EnrolledCount,
    int OrganizerId,
    string OrganizerFirstName,
    string OrganizerLastName,
    bool IsFull
);

public record OrganizerEventDto(
    int Id,
    string Title,
    DateTime Date,
    string Location,
    int MaxCapacity,
    int EnrolledCount,
    int ScannedCount,
    string? ImageUrl
);
