using Microsoft.AspNetCore.Mvc.Rendering;

namespace FitControlWeb.ViewModels.Clases;

public class ClaseIndexViewModel
{
    public List<FitControlWeb.ViewModels.ClaseListViewModel> Clases { get; set; } = new();
    public string Search { get; set; } = string.Empty;
    public int? EntrenadorId { get; set; }
    public int? EspecialidadId { get; set; }
    public string? Estado { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public bool EsCliente { get; set; }
    public bool EsEntrenador { get; set; }
    public List<SelectListItem> Entrenadores { get; set; } = new();
    public List<SelectListItem> Especialidades { get; set; } = new();

    public int StartPage => Math.Max(1, CurrentPage - 2);
    public int EndPage => Math.Min(TotalPages, CurrentPage + 2);
}
