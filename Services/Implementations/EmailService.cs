using System.Net;
using System.Net.Mail;
using FitControlWeb.Services.Interfaces;

namespace FitControlWeb.Services.Implementations;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var from = _configuration["Email:From"];
        var user = _configuration["Email:User"];
        var password = _configuration["Email:Password"];
        var smtpHost = _configuration["Email:Smtp"];
        var smtpPortRaw = _configuration["Email:Port"];

        if (string.IsNullOrWhiteSpace(from) ||
            string.IsNullOrWhiteSpace(user) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(smtpHost) ||
            string.IsNullOrWhiteSpace(smtpPortRaw))
        {
            throw new InvalidOperationException("La configuración de Email está incompleta.");
        }

        if (!int.TryParse(smtpPortRaw, out var smtpPort))
        {
            throw new InvalidOperationException("Email:Port no tiene un valor numérico válido.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(from),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(user, password)
        };

        _logger.LogInformation("Enviando email a {Recipient} con asunto {Subject}", to, subject);
        await client.SendMailAsync(message);
    }
}
