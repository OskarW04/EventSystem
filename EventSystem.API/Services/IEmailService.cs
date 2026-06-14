namespace EventSystem.API.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string to, string resetToken);
}
