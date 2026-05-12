namespace FitControlWeb.ViewModels.Shared;

public class FileContentViewModel
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
    public string FileName { get; set; } = "archivo";
    public bool Inline { get; set; }
}
