using FitControlWeb.Data;
using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace FitControlWeb.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly FitControlDbContext _context;

    public DashboardService(FitControlDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardViewModel> GetAdminDashboardAsync()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var hoyDateTime = DateTime.Today;
        var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var finMes = inicioMes.AddMonths(1);

        var totalUsuarios = await _context.Usuarios.CountAsync();
        var usuariosActivos = await _context.Usuarios.CountAsync(u => u.Activo == true);
        var usuariosInactivos = await _context.Usuarios.CountAsync(u => u.Activo != true);

        var totalClientes = await _context.Usuarios
            .CountAsync(u => u.Rol.Nombre == "Cliente");

        var totalEntrenadores = await _context.Usuarios
            .CountAsync(u => u.Rol.Nombre == "Entrenador");

        var totalAdmins = await _context.Usuarios
            .CountAsync(u => u.Rol.Nombre == "Administrador");

        var clientesSinSuscripcionActiva = await _context.Usuarios
            .CountAsync(u =>
                u.Activo == true &&
                u.Rol.Nombre == "Cliente" &&
                !u.Suscripcions.Any(s => s.Activa == true && s.FechaFin >= hoyDateTime));

        var totalClases = await _context.Clases.CountAsync();
        var clasesActivas = await _context.Clases.CountAsync(c => c.Activo == true);
        var clasesHoy = await _context.Clases.CountAsync(c => c.Fecha == hoy && c.Activo == true);

        var clasesParaOcupacion = await _context.Clases
            .Include(c => c.Reservas)
            .Where(c => c.Activo == true && c.Fecha >= hoy)
            .ToListAsync();

        var plazasTotales = clasesParaOcupacion.Sum(c => c.CapacidadMaxima ?? 0);
        var plazasOcupadas = clasesParaOcupacion.Sum(c => c.Reservas.Count(r => r.Activo == true));

        var ocupacionMedia = plazasTotales > 0
            ? Math.Round((double)plazasOcupadas * 100 / plazasTotales, 2)
            : 0;

        var clasesCasiLlenas = clasesParaOcupacion
            .Where(c => c.CapacidadMaxima > 0)
            .Where(c =>
            {
                var ocupadas = c.Reservas.Count(r => r.Activo == true);
                var capacidad = c.CapacidadMaxima ?? 1;
                var porcentaje = ocupadas * 100 / capacidad;
                return porcentaje >= 80;
            })
            .Take(5)
            .ToList();

        var clasesBajaOcupacion = clasesParaOcupacion
            .Where(c => c.CapacidadMaxima > 0)
            .Where(c =>
            {
                var ocupadas = c.Reservas.Count(r => r.Activo == true);
                var capacidad = c.CapacidadMaxima ?? 1;
                var porcentaje = ocupadas * 100 / capacidad;
                return porcentaje <= 30;
            })
            .Take(5)
            .ToList();

        var proximasClases = await _context.Clases
            .Include(c => c.Entrenador)
            .Include(c => c.Especialidad)
            .Include(c => c.Reservas)
            .Where(c => c.Activo == true && c.Fecha >= hoy)
            .OrderBy(c => c.Fecha)
            .ThenBy(c => c.HoraInicio)
            .Take(5)
            .ToListAsync();

        var totalReservas = await _context.Reservas.CountAsync();
        var reservasActivas = await _context.Reservas.CountAsync(r => r.Activo == true);
        var reservasCanceladas = await _context.Reservas.CountAsync(r => r.Activo != true);

        var ultimasReservas = await _context.Reservas
            .Include(r => r.Usuario)
            .Include(r => r.Clase)
            .Include(r => r.EstadoReserva)
            .OrderByDescending(r => r.FechaReserva)
            .Take(5)
            .ToListAsync();

        var suscripcionesActivas = await _context.Suscripciones
            .CountAsync(s => s.Activa == true && s.FechaFin >= hoyDateTime);

        var suscripcionesVencidas = await _context.Suscripciones
            .CountAsync(s => s.FechaFin < hoyDateTime || s.Activa != true);

        var suscripcionesNuevasMes = await _context.Suscripciones
            .CountAsync(s => s.FechaInicio >= inicioMes && s.FechaInicio < finMes);

        var totalFacturas = await _context.Facturas
            .CountAsync(f => f.Activo == true);

        var facturasPagadas = await _context.Facturas
            .CountAsync(f => f.Activo == true && f.Pagada == true);

        var facturasPendientes = await _context.Facturas
            .CountAsync(f => f.Activo == true && f.Pagada != true);

        var totalFacturado = await _context.Facturas
            .Where(f => f.Activo == true)
            .SumAsync(f => (decimal?)f.Total) ?? 0m;

        var totalCobrado = await _context.Pagos
            .Where(p => p.Activo == true)
            .SumAsync(p => (decimal?)p.Monto) ?? 0m;

        var pendienteCobro = await _context.Facturas
            .Where(f => f.Activo == true && f.Pagada != true)
            .SumAsync(f => (decimal?)f.Total) ?? 0m;

        var ingresosMesActual = await _context.Pagos
            .Where(p =>
                p.Activo == true &&
                p.FechaPago >= inicioMes &&
                p.FechaPago < finMes)
            .SumAsync(p => (decimal?)p.Monto) ?? 0m;

        var ultimasFacturas = await _context.Facturas
            .Include(f => f.Usuario)
            .Include(f => f.TipoFactura)
            .Where(f => f.Activo == true)
            .OrderByDescending(f => f.FechaEmision)
            .Take(5)
            .ToListAsync();

        var ultimosPagos = await _context.Pagos
            .Include(p => p.Factura)
                .ThenInclude(f => f.Usuario)
            .Include(p => p.MetodoPago)
            .Where(p => p.Activo == true)
            .OrderByDescending(p => p.FechaPago)
            .Take(5)
            .ToListAsync();

        return new DashboardViewModel
        {
            TotalUsuarios = totalUsuarios,
            TotalClientes = totalClientes,
            TotalEntrenadores = totalEntrenadores,
            TotalAdmins = totalAdmins,
            UsuariosActivos = usuariosActivos,
            UsuariosInactivos = usuariosInactivos,
            ClientesSinSuscripcionActiva = clientesSinSuscripcionActiva,

            TotalClases = totalClases,
            ClasesActivas = clasesActivas,
            ClasesHoy = clasesHoy,
            ClasesCasiLlenas = clasesCasiLlenas,
            ClasesBajaOcupacion = clasesBajaOcupacion,

            TotalReservas = totalReservas,
            ReservasActivas = reservasActivas,
            ReservasCanceladas = reservasCanceladas,
            UltimasReservas = ultimasReservas,

            PlazasTotales = plazasTotales,
            PlazasOcupadas = plazasOcupadas,
            OcupacionMedia = ocupacionMedia,
            ProximasClases = proximasClases,

            SuscripcionesActivas = suscripcionesActivas,
            SuscripcionesVencidas = suscripcionesVencidas,
            SuscripcionesNuevasMes = suscripcionesNuevasMes,

            TotalFacturas = totalFacturas,
            FacturasPagadas = facturasPagadas,
            FacturasPendientes = facturasPendientes,
            TotalFacturado = totalFacturado,
            TotalCobrado = totalCobrado,
            PendienteCobro = pendienteCobro,
            IngresosMesActual = ingresosMesActual,
            UltimasFacturas = ultimasFacturas,
            UltimosPagos = ultimosPagos
        };
    }
}
