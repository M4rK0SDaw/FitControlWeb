using FitControlWeb.Models.Entities;

namespace FitControlWeb.ViewModels.Dashboard;

public class DashboardViewModel
{
    //  Usuarios
    public int TotalUsuarios { get; set; }
    public int TotalClientes { get; set; }
    public int TotalEntrenadores { get; set; }
    public int UsuariosActivos { get; set; }
    public int UsuariosInactivos { get; set; }

    // 🏋 Clases
    public int TotalClases { get; set; }
    public int ClasesActivas { get; set; }
    public int ClasesHoy { get; set; }
    
    //  Reservas
    public int TotalReservas { get; set; }
    public int ReservasActivas { get; set; }
    public int ReservasCanceladas { get; set; }

    //  Métricas útiles
    public double OcupacionMedia { get; set; } // %
    public int PlazasTotales { get; set; }
    public int PlazasOcupadas { get; set; }

    //  Listados
    public List<Clase> ProximasClases { get; set; } = new();
    public List<Reserva> UltimasReservas { get; set; } = new();

    public int TotalAdmins { get; set; }

    public List<Clase> ClasesCasiLlenas { get; set; } = new();
    public List<Clase> ClasesBajaOcupacion { get; set; } = new();


    // Económico
    public int TotalFacturas { get; set; }
    public int FacturasPagadas { get; set; }
    public int FacturasPendientes { get; set; }

    public decimal TotalFacturado { get; set; }
    public decimal TotalCobrado { get; set; }
    public decimal PendienteCobro { get; set; }
    public decimal IngresosMesActual { get; set; }

    public List<Factura> UltimasFacturas { get; set; } = new();
    public List<Pago> UltimosPagos { get; set; } = new();
}