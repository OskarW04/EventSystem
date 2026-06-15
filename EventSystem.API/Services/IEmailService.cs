namespace EventSystem.API.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string to, string resetToken);

    Task SendOrganizationTokenEmailAsync(string to, string token);
}
