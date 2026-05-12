using FitControlWeb.Models.Entities;

namespace FitControlWeb.ViewModels.Facturas;

public class FacturaIndexViewModel
{
    public List<Factura> Facturas { get; set; } = new();
    public string? Search { get; set; }
    public bool? Pagada { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalFacturas { get; set; }
    public int TotalPagadas { get; set; }
    public int TotalPendientes { get; set; }
    public decimal ImportePagina { get; set; }

    public int StartPage => Math.Max(1, CurrentPage - 2);
    public int EndPage => Math.Min(TotalPages, CurrentPage + 2);
}
