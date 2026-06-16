namespace EventSystem.API.DTOs;

public record TicketDto(
    int Id,
    int EventId,
    string EventTitle,
    DateTime EventDate,
    DateTime StartDate,      // = data rozpoczęcia wydarzenia (alias EventDate)
    DateTime? EndDate,       // data zakończenia wydarzenia (jeśli ustawiona)
    string Location,
    string QrCodeContent,
    bool IsScanned,
    int StudentId,
    bool IsExpired           // wydarzenie się zakończyło → bilet przedawniony
);

public record ScanResultDto(
    int TicketId,
    string StudentFirstName,
    string StudentLastName,
    string EventTitle,
    DateTime ScannedAt
);