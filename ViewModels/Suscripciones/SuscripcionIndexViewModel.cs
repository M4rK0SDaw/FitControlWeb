using FitControlWeb.Models.Entities;
using FitControlWeb.ViewModels.Shared;

namespace FitControlWeb.ViewModels.Suscripciones;

public class SuscripcionIndexViewModel
{
    public List<Suscripcion> Suscripciones { get; set; } = new();
    public string? Search { get; set; }
    public string? Estado { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalSuscripciones { get; set; }
    public int ActivasPagina { get; set; }
    public int InactivasPagina { get; set; }
    public int VencidasPagina { get; set; }

    public int StartPage => Math.Max(1, CurrentPage - 2);
    public int EndPage => Math.Min(TotalPages, CurrentPage + 2);

    public ExportButtonsViewModel ExportButtons => new()
    {
        Controller = "Suscripciones",
        ExcelAction = "ExportExcel",
        CsvAction = "ExportCsv",
        PdfAction = "ExportPdf",
        RouteValues = new Dictionary<string, string?>
        {
            { "search", Search },
            { "estado", Estado }
        }
    };
}
