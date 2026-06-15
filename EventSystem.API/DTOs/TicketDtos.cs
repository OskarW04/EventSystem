namespace EventSystem.API.DTOs;

public record TicketDto(
    int Id,
    int EventId,
    string EventTitle,
    DateTime EventDate,
    string Location,
    string QrCodeContent,
    bool IsScanned,
    int StudentId
);

public record ScanResultDto(
    int TicketId,
    string StudentFirstName,
    string StudentLastName,
    string EventTitle,
    DateTime ScannedAt
);