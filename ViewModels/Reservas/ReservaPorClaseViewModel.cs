using FitControlWeb.Models.Entities;

namespace FitControlWeb.ViewModels.Reservas;

public class ReservaPorClaseViewModel
{
    public List<Reserva> Reservas { get; set; } = new();
    public int ClaseId { get; set; }
    public string? Search { get; set; }
    public string? Estado { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalReservas { get; set; }
    public int PlazasOcupadas { get; set; }
    public int CapacidadMaxima { get; set; }
    public int PlazasCanceladas { get; set; }
    public string NombreClase { get; set; } = "Clase";
    public DateTime? FechaClase { get; set; }
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFin { get; set; }

    public int StartPage => Math.Max(1, CurrentPage - 2);
    public int EndPage => Math.Min(TotalPages, CurrentPage + 2);
}
