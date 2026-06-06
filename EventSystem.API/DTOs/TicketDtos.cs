namespace EventSystem.API.DTOs;

// Nazwy muszą pasować do interfejsu Ticket w React!
public record TicketDto(
    int Id, 
    string EventTitle, 
    DateTime EventDate, 
    string EventLocation, // To naprawi brakującą lokalizację
    string QrCodeContent, 
    bool IsUsed           // W bazie masz IsScanned, ale frontend czyta isUsed
);