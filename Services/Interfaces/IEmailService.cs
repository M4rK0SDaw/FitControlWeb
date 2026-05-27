namespace FitControlWeb.Services.Interfaces;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody);
    Task SendWithAttachmentAsync(string to, string subject, string htmlBody, byte[] attachmentContent, string attachmentFileName, string contentType);
}
