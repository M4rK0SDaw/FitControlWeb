using FitControlWeb.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FitControlWeb.ViewModels.Usuarios;

public class UsuarioIndexViewModel
{
    public List<UsuarioListViewModel> Usuarios { get; set; } = new();
    public List<SelectListItem> Roles { get; set; } = new();
    public string? Search { get; set; }
    public int? RolId { get; set; }
    public bool? Activo { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalUsuarios { get; set; }
    public int UsuariosActivos { get; set; }
    public int UsuariosInactivos { get; set; }
    public int TotalClientes { get; set; }
    public int TotalEntrenadores { get; set; }
    public int TotalAdministradores { get; set; }
    public int NuevosEsteMes { get; set; }

    public int StartPage => Math.Max(1, CurrentPage - 2);
    public int EndPage => Math.Min(TotalPages, CurrentPage + 2);

    public ExportButtonsViewModel ExportButtons => new()
    {
        Controller = "Usuarios",
        ExcelAction = "ExportExcel",
        CsvAction = "ExportCsv",
        PdfAction = "ExportPdf",
        RouteValues = new Dictionary<string, string?>
        {
            { "search", Search },
            { "rolId", RolId?.ToString() },
            { "activo", Activo?.ToString() }
        }
    };
}
