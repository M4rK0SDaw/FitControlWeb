using FitControlWeb.Models.Entities;

namespace FitControlWeb.ViewModels.Reservas;

public class ReservaIndexViewModel
{
    public List<Reserva> Reservas { get; set; } = new();
    public string? Search { get; set; }
    public string? Estado { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalReservas { get; set; }
    public int PlazasCanceladas { get; set; }

    public int StartPage => Math.Max(1, CurrentPage - 2);
    public int EndPage => Math.Min(TotalPages, CurrentPage + 2);
    public int TotalActivas => TotalReservas - PlazasCanceladas;
}
