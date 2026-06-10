using System.ComponentModel.DataAnnotations;

namespace EventSystem.API.DTOs;

public record CreateEventDto(
    [Required, StringLength(100, MinimumLength = 3)] string Title,
    [Required, StringLength(5000, MinimumLength = 10)] string Description,
    [Required] DateTime Date,
    [Required, StringLength(200, MinimumLength = 2)] string Location,
    [Range(1, 100000)] int MaxCapacity
);

public record UpdateEventDto(
    [Required, StringLength(100, MinimumLength = 3)] string Title,
    [Required, StringLength(5000, MinimumLength = 10)] string Description,
    [Required] DateTime Date,
    [Required, StringLength(200, MinimumLength = 2)] string Location,
    [Range(1, 100000)] int MaxCapacity
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