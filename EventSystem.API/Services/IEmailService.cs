namespace EventSystem.API.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string to, string resetToken);

    Task SendOrganizationTokenEmailAsync(string to, string token);

    // #4 - powiadomienie pre-saverów o otwarciu rejestracji na wydarzenie.
    Task SendRegistrationOpenEmailAsync(string to, int eventId, string eventTitle);
}
