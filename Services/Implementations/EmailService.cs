using System.Net;
using System.Net.Mail;
using FitControlWeb.Services.Interfaces;

namespace FitControlWeb.Services.Implementations;

public class EmailService : IEmailService
{
    private const string BrandName = "FitControl Web";
    private const string BrandSupportEmail = "soporte@fitcontrolweb.local";
    private const string BrandPhone = "+34 900 123 456";
    private const string BrandAddress = "Centro de gestion FitControl";
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
            From = new MailAddress(from, BrandName),
            Subject = subject,
            Body = BuildEmailLayout(subject, htmlBody),
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

    private static string BuildEmailLayout(string subject, string contentHtml)
    {
        var safeSubject = WebUtility.HtmlEncode(subject);

        return $$"""
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>{{safeSubject}}</title>
</head>
<body style="margin:0;padding:0;background-color:#fff4eb;font-family:Arial,Helvetica,sans-serif;color:#1f2937;">
    <table role="presentation" style="width:100%;border-collapse:collapse;background-color:#fff4eb;">
        <tr>
            <td align="center" style="padding:32px 16px;">
                <table role="presentation" style="width:100%;max-width:680px;border-collapse:collapse;background-color:#ffffff;border:1px solid #ffd1ad;border-radius:18px;overflow:hidden;box-shadow:0 12px 30px rgba(124,45,18,0.12);">
                    <tr>
                        <td style="padding:0;background-color:#ff7a00;">
                            <table role="presentation" style="width:100%;border-collapse:collapse;">
                                <tr>
                                    <td style="padding:26px 32px;color:#ffffff;">
                                        <div style="font-size:12px;letter-spacing:1.6px;text-transform:uppercase;opacity:0.9;font-weight:700;">Gestion integral del gimnasio</div>
                                        <div style="margin-top:10px;font-size:28px;font-weight:700;line-height:1.2;">{{BrandName}}</div>
                                        <div style="margin-top:8px;font-size:14px;line-height:1.6;opacity:0.92;">{{safeSubject}}</div>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td style="padding:32px;border-top:4px solid #ffb36b;">
                            <div style="font-size:15px;line-height:1.75;color:#334155;">
                                {{contentHtml}}
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style="padding:0 32px 20px 32px;">
                            <div style="height:1px;background-color:#ffd9bd;"></div>
                        </td>
                    </tr>
                    <tr>
                        <td style="padding:0 32px 32px 32px;">
                            <table role="presentation" style="width:100%;border-collapse:collapse;background-color:#fff7f0;border:1px solid #ffd9bd;border-radius:14px;">
                                <tr>
                                    <td style="padding:20px 22px;">
                                        <div style="font-size:15px;font-weight:700;color:#0f172a;">Atentamente,</div>
                                        <div style="margin-top:4px;font-size:14px;font-weight:700;color:#ea580c;">Equipo {{BrandName}}</div>
                                        <div style="margin-top:12px;font-size:13px;line-height:1.7;color:#475569;">
                                            {{BrandAddress}}<br />
                                            <a href="mailto:{{BrandSupportEmail}}" style="color:#ea580c;text-decoration:none;font-weight:600;">{{BrandSupportEmail}}</a> | {{BrandPhone}}
                                        </div>
                                        <div style="margin-top:14px;font-size:12px;line-height:1.7;color:#64748b;">
                                            Este mensaje se ha enviado desde la plataforma de gestion FitControl Web. Si necesitas ayuda, responde a este correo o contacta con soporte.
                                        </div>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
""";
    }
}
