namespace EventSystem.API.DTOs;

public record LoginDto(string Email, string Password);

public record RegisterStudentDto(string FirstName, string LastName, string Email, string Password);

public record RegisterOrganizerDto(string FirstName, string LastName, string Email, string Password, string Token);