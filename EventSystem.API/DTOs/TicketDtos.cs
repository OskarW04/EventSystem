namespace EventSystem.API.DTOs;

public record TicketDto(
    int Id,
    string EventTitle,
    DateTime EventDate,
    string Location,
    string QrCodeContent,
    bool IsScanned
);

public record ScanResultDto(
    int TicketId,
    string StudentFirstName,
    string StudentLastName,
    string EventTitle,
    DateTime ScannedAt
);