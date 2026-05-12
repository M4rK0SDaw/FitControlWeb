using System.Net;
using FitControlWeb.Services.Interfaces;
using FitControlWeb.Services.Models;

namespace FitControlWeb.Services.Implementations;

public class EmailTemplateService : IEmailTemplateService
{
    public EmailTemplateMessage EmailBienvenida(string nombre)
    {
        var safeName = Encode(nombre);

        return new EmailTemplateMessage
        {
            Subject = "Bienvenido a FitControl Web",
            HtmlBody = $$"""
                <p>Hola {{safeName}},</p>
                <p>Te damos la bienvenida a <strong>FitControl Web</strong>. Tu cuenta ya esta lista para gestionar tu actividad en el gimnasio de forma comoda y centralizada.</p>
                <p>Desde este momento puedes consultar tus clases, revisar tus reservas y mantener el contacto con tu entrenador desde la plataforma.</p>
                <p>Nos alegra tenerte dentro.</p>
                """
        };
    }

    public EmailTemplateMessage EmailCuentaBloqueada(string nombre, string resetLink)
    {
        var safeName = Encode(nombre);
        var safeLink = Encode(resetLink);

        return new EmailTemplateMessage
        {
            Subject = "Cuenta bloqueada - Recuperacion FitControl",
            HtmlBody = $$"""
                <p>Hola {{safeName}},</p>
                <p>Hemos bloqueado temporalmente tu cuenta tras varios intentos de acceso fallidos para proteger tu informacion.</p>
                <p>Puedes recuperarla desde este enlace seguro:</p>
                <p><a href="{{safeLink}}" style="display:inline-block;padding:10px 18px;background-color:#ff7a00;color:#ffffff;text-decoration:none;border-radius:10px;font-weight:600;">Recuperar cuenta</a></p>
                <p>El enlace estara disponible durante 1 minuto.</p>
                <p>Si no reconoces esta situacion, te recomendamos cambiar tu contrasena cuanto antes.</p>
                """
        };
    }

    public EmailTemplateMessage EmailRestablecerContrasenya(string nombre, string resetLink)
    {
        var safeName = Encode(nombre);
        var safeLink = Encode(resetLink);

        return new EmailTemplateMessage
        {
            Subject = "Restablecer contrasena - FitControl Web",
            HtmlBody = $$"""
                <p>Hola {{safeName}},</p>
                <p>Hemos recibido una solicitud para restablecer tu contrasena en <strong>FitControl Web</strong>.</p>
                <p>Cuando quieras, puedes continuar desde aqui:</p>
                <p><a href="{{safeLink}}" style="display:inline-block;padding:10px 18px;background-color:#ff7a00;color:#ffffff;text-decoration:none;border-radius:10px;font-weight:600;">Restablecer contrasena</a></p>
                <p>Este enlace caduca en 1 minuto. Si no solicitaste este cambio, puedes ignorar este mensaje con tranquilidad.</p>
                """
        };
    }

    public EmailTemplateMessage EmailAdminDirecto(string nombre, string subject, string message)
    {
        var safeName = Encode(nombre);
        var safeMessage = Encode(message).Replace("\n", "<br />");

        return new EmailTemplateMessage
        {
            Subject = subject.Trim(),
            HtmlBody = $$"""
                <p>Hola {{safeName}},</p>
                <p>{{safeMessage}}</p>
                <p>Quedamos a tu disposicion para cualquier consulta adicional.</p>
                """
        };
    }

    private static string Encode(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }
}
