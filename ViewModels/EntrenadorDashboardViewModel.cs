using FitControlWeb.Models.Entities;

namespace FitControlWeb.ViewModels.Dashboard;

public class EntrenadorDashboardViewModel
{
    public Usuario? Entrenador { get; set; }

    public int TotalClases { get; set; }
    public int ClasesHoy { get; set; }
    public int ProximasClases { get; set; }
    public int TotalReservas { get; set; }

    public int PlazasTotales { get; set; }
    public int PlazasOcupadas { get; set; }
    public double OcupacionMedia { get; set; }

    public List<Clase> ClasesDeHoy { get; set; } = new();
    public List<Clase> ProximasClasesListado { get; set; } = new();
    public List<Reserva> UltimasReservas { get; set; } = new();
}