using System.Net;
using FitControlWeb.Models.Entities;
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
                <p>El enlace estara disponible durante 15 minutos.</p>
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
                <p>Este enlace caduca en 15 minutos. Si no solicitaste este cambio, puedes ignorar este mensaje con tranquilidad.</p>
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

    public EmailTemplateMessage EmailPagoFactura(string nombre, Factura factura)
    {
        var safeName = Encode(nombre);
        var safeNumero = Encode(factura.NumeroFactura);
        var fecha = Encode((factura.FechaEmision ?? DateTime.Now).ToString("dd/MM/yyyy"));
        var total = Encode(factura.Total.ToString("0.00"));

        return new EmailTemplateMessage
        {
            Subject = $"Pago confirmado - Factura {factura.NumeroFactura}",
            HtmlBody = $$"""
                <p>Hola {{safeName}},</p>
                <p>Hemos confirmado correctamente el pago de tu factura <strong>{{safeNumero}}</strong>.</p>
                <table role="presentation" style="width:100%;border-collapse:collapse;margin:18px 0;background-color:#fff7f0;border:1px solid #ffd9bd;border-radius:12px;overflow:hidden;">
                    <tr>
                        <td style="padding:12px 16px;color:#64748b;">Fecha</td>
                        <td style="padding:12px 16px;text-align:right;font-weight:700;color:#111827;">{{fecha}}</td>
                    </tr>
                    <tr>
                        <td style="padding:12px 16px;color:#64748b;border-top:1px solid #ffd9bd;">Total abonado</td>
                        <td style="padding:12px 16px;text-align:right;font-weight:800;color:#ea580c;border-top:1px solid #ffd9bd;">{{total}} EUR</td>
                    </tr>
                </table>
                <p>La factura queda disponible en tu panel para consultarla, visualizarla o descargarla cuando lo necesites.</p>
                <p>Gracias por confiar en FitControl Web.</p>
                """
        };
    }

    private static string Encode(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }
}
