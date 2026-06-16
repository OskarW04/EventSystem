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
    [Range(1, 100000)] int MaxCapacity,
    // #4 - null = rejestracja otwarta od razu
    DateTime? RegistrationOpensAt,
    // #4 - null = pre-rejestracja dostępna od razu
    DateTime? PresaveOpensAt
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => EventRegistrationWindow.Validate(Date, RegistrationOpensAt, PresaveOpensAt);
}

public record UpdateEventDto(
    [Required, StringLength(100, MinimumLength = 3)] string Title,
    [Required, StringLength(5000, MinimumLength = 10)] string Description,
    [Required] DateTime Date,
    DateTime? EndDate,
    [Required, StringLength(200, MinimumLength = 2)] string Location,
    [StringLength(200)] string? LocationName,
    double? Lat,
    double? Lng,
    [Range(1, 100000)] int MaxCapacity,
    // #4 - null = rejestracja otwarta od razu
    DateTime? RegistrationOpensAt,
    // #4 - null = pre-rejestracja dostępna od razu
    DateTime? PresaveOpensAt
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => EventRegistrationWindow.Validate(Date, RegistrationOpensAt, PresaveOpensAt);
}


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
    int EnrolledCount,
    int Clicks24h,                  // #6/#5 - odsłony z ostatnich 24h
    DateTime? RegistrationOpensAt,  // #4 - null = otwarte
    DateTime? PresaveOpensAt,       // #4 - null = pre-save od razu
    bool HasPresaved                // #4 - czy zalogowany student już pre-savnął
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
    bool IsFull,
    int Clicks24h,                  // #6/#5
    DateTime? RegistrationOpensAt,  // #4
    DateTime? PresaveOpensAt,       // #4 - null = pre-save od razu
    bool HasPresaved                // #4
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
    string? ImageUrl,
    int Clicks24h,                  // #6/#5
    DateTime? RegistrationOpensAt,  // #4
    DateTime? PresaveOpensAt,       // #4 - null = pre-save od razu
    int PresaveCount                // #4 - ile osób czeka na otwarcie
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
    string OrganizerName,
    int Clicks24h,                  // #6/#5
    DateTime? RegistrationOpensAt,  // #4
    DateTime? PresaveOpensAt,       // #4 - null = pre-save od razu
    int PresaveCount                // #4
);
