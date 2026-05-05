using FitControlWeb.Models.Entities;

namespace FitControlWeb.ViewModels.Dashboard;

public class ClienteDashboardViewModel
{
    public Usuario? Usuario { get; set; }

    public Suscripcion? SuscripcionActual { get; set; }

    public List<Reserva> ProximasReservas { get; set; } = new();

    public List<Factura> FacturasPendientes { get; set; } = new();

    public List<Clase> ClasesDisponibles { get; set; } = new();

    public int TotalReservasActivas { get; set; }

    public int TotalFacturasPendientes { get; set; }

    public decimal ImportePendiente { get; set; }
}