using FitControlWeb.Services.Models;

namespace FitControlWeb.Services.Interfaces;

public interface IEmailTemplateService
{
    EmailTemplateMessage EmailBienvenida(string nombre); // BuildWelcomeEmail(string nombre);
    EmailTemplateMessage EmailCuentaBloqueada(string nombre, string resetLink); // BuildAccountLockedEmail(string nombre, string resetLink);
    EmailTemplateMessage EmailRestablecerContrasenya(string nombre, string resetLink); // BuildResetPasswordEmail(string nombre, string resetLink);
    EmailTemplateMessage EmailAdminDirecto(string nombre, string subject, string message); // BuildAdminDirectEmail(string nombre, string subject, string message);
}
