namespace EventSystem.API.DTOs;

public record TicketDto(int Id, string EventTitle, DateTime EventDate, string Location, string QrCodeContent, bool IsScanned);