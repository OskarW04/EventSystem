namespace EventSystem.API.DTOs;

public record CreateEventDto(
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