using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EventSystem.API.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string to, string resetToken)
    {
        var frontendUrl = _config["AppSettings:FrontendUrl"]?.TrimEnd('/');
        var encodedToken = Uri.EscapeDataString(resetToken);
        var encodedEmail = Uri.EscapeDataString(to);
        var resetLink = $"{frontendUrl}/reset-password?token={encodedToken}&email={encodedEmail}";

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_config["EmailSettings:From"]));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = "Resetowanie hasła — EventSystem";
        message.Body = new TextPart("html")
        {
            Text = $"""
                <div style="font-family: Arial, sans-serif; max-width: 480px; margin: 0 auto;">
                    <h2>Resetowanie hasła</h2>
                    <p>Otrzymaliśmy prośbę o zresetowanie hasła dla Twojego konta w EventSystem.</p>
                    <p>Kliknij poniższy przycisk, aby ustawić nowe hasło. Link jest ważny przez <strong>1 godzinę</strong>.</p>
                    <p style="margin: 24px 0;">
                        <a href="{resetLink}"
                           style="background:#4f46e5;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;font-weight:bold;">
                            Resetuj hasło
                        </a>
                    </p>
                    <p style="color:#6b7280;font-size:13px;">
                        Jeśli nie prosiłeś o reset hasła, zignoruj tę wiadomość — Twoje hasło pozostanie bez zmian.
                    </p>
                </div>
                """
        };

        await SendAsync(message);

        _logger.LogInformation("Password reset email sent to {Email}", to);
    }

    public async Task SendOrganizationTokenEmailAsync(string to, string token)
    {
        var frontendUrl = _config["AppSettings:FrontendUrl"]?.TrimEnd('/');
        var registerLink = $"{frontendUrl}/register-organizer";

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_config["EmailSettings:From"]));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = "Token rejestracyjny Organizatora — EventSystem";
        message.Body = new TextPart("html")
        {
            Text = $"""
                <div style="font-family: Arial, sans-serif; max-width: 480px; margin: 0 auto;">
                    <h2>Zaproszenie dla Organizatora</h2>
                    <p>Otrzymujesz token rejestracyjny, który pozwala założyć konto Organizatora w EventSystem.</p>
                    <p>Twój token:</p>
                    <p style="margin: 16px 0;">
                        <code style="display:inline-block;background:#f3f4f6;border:1px solid #e5e7eb;border-radius:6px;padding:12px 16px;font-size:15px;font-weight:bold;letter-spacing:1px;word-break:break-all;">
                            {token}
                        </code>
                    </p>
                    <p>Przejdź do strony rejestracji i wklej token w odpowiednie pole:</p>
                    <p style="margin: 24px 0;">
                        <a href="{registerLink}"
                           style="background:#4f46e5;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;font-weight:bold;">
                            Zarejestruj konto Organizatora
                        </a>
                    </p>
                    <p style="color:#6b7280;font-size:13px;">
                        Jeśli nie oczekiwałeś tej wiadomości, po prostu ją zignoruj.
                    </p>
                </div>
                """
        };

        await SendAsync(message);

        _logger.LogInformation("Organization token email sent to {Email}", to);
    }

    private async Task SendAsync(MimeMessage message)
    {
        using var client = new SmtpClient();

        await client.ConnectAsync(
            _config["EmailSettings:Host"],
            int.Parse(_config["EmailSettings:Port"] ?? "587"),
            SecureSocketOptions.StartTls);

        await client.AuthenticateAsync(
            _config["EmailSettings:Username"],
            _config["EmailSettings:Password"]);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
