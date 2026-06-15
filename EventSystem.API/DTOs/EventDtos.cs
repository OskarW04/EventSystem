using System.ComponentModel.DataAnnotations;

namespace EventSystem.API.DTOs;

public record CreateEventDto(
    [Required, StringLength(100, MinimumLength = 3)] string Title,
    [Required, StringLength(5000, MinimumLength = 10)] string Description,
    [Required] DateTime Date,
    DateTime? EndDate,
    [Required, StringLength(200, MinimumLength = 2)] string Location,
    [StringLength(200)] string? LocationName,
    double? Lat,
    double? Lng,
    [Range(1, 100000)] int MaxCapacity
);

public record UpdateEventDto(
    [Required, StringLength(100, MinimumLength = 3)] string Title,
    [Required, StringLength(5000, MinimumLength = 10)] string Description,
    [Required] DateTime Date,
    DateTime? EndDate,
    [Required, StringLength(200, MinimumLength = 2)] string Location,
    [StringLength(200)] string? LocationName,
    double? Lat,
    double? Lng,
    [Range(1, 100000)] int MaxCapacity
);


public record EventDto(
    int Id,
    string Title,
    string Description,
    DateTime Date,
    DateTime? EndDate,
    string Location,
    string? LocationName,
    double? Lat,
    double? Lng,
    int MaxCapacity,
    string? ImageUrl,
    int EnrolledCount
);

public record EventDetailsDto(
    int Id,
    string Title,
    string Description,
    DateTime Date,
    DateTime? EndDate,
    string Location,
    string? LocationName,
    double? Lat,
    double? Lng,
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
    DateTime? EndDate,
    string Location,
    string? LocationName,
    double? Lat,
    double? Lng,
    int MaxCapacity,
    int EnrolledCount,
    int ScannedCount,
    string? ImageUrl
);

public record AdminEventDto(
    int Id,
    string Title,
    string Description,
    DateTime Date,
    DateTime? EndDate,
    string Location,
    string? LocationName,
    double? Lat,
    double? Lng,
    int MaxCapacity,
    string? ImageUrl,
    int EnrolledCount,
    int ScannedCount,
    int OrganizerId,
    string OrganizerFirstName,
    string OrganizerLastName,
    string OrganizerEmail,
    string OrganizerName
);
