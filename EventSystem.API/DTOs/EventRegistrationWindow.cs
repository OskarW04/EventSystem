using System.ComponentModel.DataAnnotations;

namespace EventSystem.API.DTOs;

// Walidacja okna zapisów (#4). Reguła: PresaveOpensAt <= RegistrationOpensAt <= Date,
// o ile dane pole jest ustawione. Współdzielona przez CreateEventDto i UpdateEventDto
// (IValidatableObject), więc niespójne okno zwraca 400 zanim trafi do bazy.
//
// Porównujemy surowe wartości z DTO (przed ToUniversalTime) - mają ten sam Kind
// (Unspecified, czas lokalny fronta), więc porównania względne są poprawne.
public static class EventRegistrationWindow
{
    public static IEnumerable<ValidationResult> Validate(
        DateTime startDate, DateTime? registrationOpensAt, DateTime? presaveOpensAt)
    {
        if (registrationOpensAt.HasValue && registrationOpensAt.Value > startDate)
            yield return new ValidationResult(
                "Otwarcie rejestracji nie może być po rozpoczęciu wydarzenia.",
                new[] { nameof(CreateEventDto.RegistrationOpensAt) });

        if (presaveOpensAt.HasValue && registrationOpensAt.HasValue
            && presaveOpensAt.Value > registrationOpensAt.Value)
            yield return new ValidationResult(
                "Otwarcie pre-rejestracji nie może być po otwarciu rejestracji.",
                new[] { nameof(CreateEventDto.PresaveOpensAt) });

        if (presaveOpensAt.HasValue && presaveOpensAt.Value > startDate)
            yield return new ValidationResult(
                "Otwarcie pre-rejestracji nie może być po rozpoczęciu wydarzenia.",
                new[] { nameof(CreateEventDto.PresaveOpensAt) });
    }
}
