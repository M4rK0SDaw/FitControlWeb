namespace FitControlWeb.Helpers;

public static class ExportFileNameHelper
{
    public static string Build(string prefix, string extension)
    {
        return $"{Sanitize(prefix)}_{DateTime.Now:yyyyMMdd_HHmm}.{extension.TrimStart('.')}";
    }

    public static string BuildFactura(string numeroFactura)
    {
        return $"factura_{Sanitize(numeroFactura)}_{DateTime.Now:yyyyMMdd}.pdf";
    }

    private static string Sanitize(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(value
            .Select(c => invalidChars.Contains(c) ? '-' : c)
            .ToArray())
            .Replace(' ', '_');
    }
}
