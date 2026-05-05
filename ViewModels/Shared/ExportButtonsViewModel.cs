namespace FitControlWeb.ViewModels.Shared;

public class ExportButtonsViewModel
{
    public string Controller { get; set; } = string.Empty;

    public string ExcelAction { get; set; } = string.Empty;

    public string CsvAction { get; set; } = string.Empty;

    public string PdfAction { get; set; } = string.Empty;

    public Dictionary<string, string?> RouteValues { get; set; } = new();

    public bool ShowExcel { get; set; } = true;
    public bool ShowCsv { get; set; } = true;
    public bool ShowPdf { get; set; } = true;
}