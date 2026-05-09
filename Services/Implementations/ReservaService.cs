using FitControlWeb.Data;
using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FitControlWeb.Services.Implementations;

public class ReservaService : IReservaService
{
    private readonly FitControlDbContext _context;

    public ReservaService(FitControlDbContext context)
    {
        _context = context;
    }

    public async Task<List<Reserva>> GetByUsuarioAsync(int usuarioId)
    {
        return await _context.Reservas
            .Include(r => r.Clase)
            .Include(r => r.EstadoReserva)
            .Where(r => r.UsuarioId == usuarioId && r.Activo == true)
            .OrderByDescending(r => r.FechaReserva)
            .ToListAsync();
    }

    public async Task<List<Reserva>> GetAllAsync()
    {
        return await _context.Reservas
            .Include(r => r.Usuario)
            .Include(r => r.Clase)
            .Include(r => r.EstadoReserva)
            .Where(r => r.Activo == true)
            .OrderByDescending(r => r.FechaReserva)
            .ToListAsync();
    }

    public async Task<List<Reserva>> GetByClaseAsync(int claseId)
    {
        return await _context.Reservas
            .Include(r => r.Usuario)
            .Include(r => r.Clase)
            .Include(r => r.EstadoReserva)
            .Where(r => r.ClaseId == claseId && r.Activo == true)
            .OrderByDescending(r => r.FechaReserva)
            .ToListAsync();
    }

    public async Task<Reserva?> GetByIdAsync(int id)
    {
        return await _context.Reservas
            .Include(r => r.Usuario)
            .Include(r => r.Clase)
            .Include(r => r.EstadoReserva)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<ServiceResult> CrearAsync(int usuarioId, int claseId)
    {
        var clase = await _context.Clases
            .FirstOrDefaultAsync(c => c.Id == claseId && c.Activo == true);

        if (clase == null)
            return ServiceResult.Fail("La clase no existe o no está activa.", "CLASE");

        if (clase.Fecha < DateOnly.FromDateTime(DateTime.Today))
            return ServiceResult.Fail("No puedes reservar una clase pasada.", "CLASE");

        var tieneSuscripcionActiva = await _context.Suscripciones.AnyAsync(s =>
            s.UsuarioId == usuarioId &&
            s.Activa == true &&
            s.FechaFin >= DateTime.Today);

        if (!tieneSuscripcionActiva)
            return ServiceResult.Fail("Necesitas una suscripción activa para reservar clases.", "SUSCRIPCION");

        var reservaExistente = await _context.Reservas
            .FirstOrDefaultAsync(r =>
                r.UsuarioId == usuarioId &&
                r.ClaseId == claseId);

        if (reservaExistente != null && reservaExistente.Activo == true)
            return ServiceResult.Fail("Ya tienes una reserva activa en esta clase.", "RESERVA");

        if (await ClienteTieneSolapeAsync(usuarioId, clase))
        {
            return ServiceResult.Fail(
                "Ya tienes una reserva activa en otra clase que coincide con este horario.",
                "SOLAPE");
        }

        if (!await HayPlazasAsync(claseId))
            return ServiceResult.Fail("No hay plazas disponibles.", "PLAZAS");

        var estadoActiva = await _context.EstadoReservas
            .FirstOrDefaultAsync(e => e.Nombre == "Activa");

        if (estadoActiva == null)
            return ServiceResult.Fail("No existe el estado 'Activa' en la base de datos.", "ESTADO");

        if (reservaExistente != null && reservaExistente.Activo != true)
        {
            reservaExistente.EstadoReservaId = estadoActiva.Id;
            reservaExistente.Activo = true;
            reservaExistente.FechaBaja = null;
            reservaExistente.FechaReserva = DateTime.Now;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Reserva realizada correctamente.");
        }

        var reserva = new Reserva
        {
            UsuarioId = usuarioId,
            ClaseId = claseId,
            EstadoReservaId = estadoActiva.Id,
            FechaReserva = DateTime.Now,
            Activo = true
        };

        _context.Reservas.Add(reserva);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Reserva realizada correctamente.");
    }
    public async Task<ServiceResult> CancelarAsync(int reservaId)
    {
        var reserva = await _context.Reservas
            .FirstOrDefaultAsync(r => r.Id == reservaId);

        if (reserva == null)
            return ServiceResult.Fail("La reserva no existe.", "RESERVA");

        if (reserva.Activo != true)
            return ServiceResult.Fail("La reserva ya está cancelada.", "CANCELADA");

        var estadoCancelada = await _context.EstadoReservas
            .FirstOrDefaultAsync(e => e.Nombre == "Cancelada");

        if (estadoCancelada == null)
            return ServiceResult.Fail("No existe el estado 'Cancelada' en la base de datos.", "ESTADO");

        reserva.EstadoReservaId = estadoCancelada.Id;
        reserva.Activo = false;
        reserva.FechaBaja = DateTime.Now;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Reserva cancelada correctamente.");
    }

    public async Task<ServiceResult> ReactivarAsync(int reservaId)
    {
        var reserva = await _context.Reservas
            .Include(r => r.Clase)
            .FirstOrDefaultAsync(r => r.Id == reservaId);

        if (reserva == null)
            return ServiceResult.Fail("La reserva no existe.", "RESERVA");

        if (reserva.Activo == true)
            return ServiceResult.Fail("La reserva ya está activa.", "ACTIVA");

        if (reserva.Clase == null || reserva.Clase.Activo != true)
            return ServiceResult.Fail("La clase no existe o no está activa.", "ACTIVA");

        if (reserva.Clase.Fecha < DateOnly.FromDateTime(DateTime.Today))
            return ServiceResult.Fail("No puedes reactivar una reserva de una clase pasada.", "CLASE");

        var reservasActivas = await _context.Reservas
            .CountAsync(r => r.ClaseId == reserva.ClaseId && r.Activo == true);

        var capacidadMaxima = reserva.Clase.CapacidadMaxima ?? 0;

        if (capacidadMaxima <= 0 || reservasActivas >= capacidadMaxima)
            return ServiceResult.Fail("No hay plazas disponibles para reactivar esta reserva.", "PLAZAS");

        if (await ClienteTieneSolapeAsync(reserva.UsuarioId, reserva.Clase, reserva.Id))
        {
            return ServiceResult.Fail(
                "No puedes reactivar esta reserva porque coincide con otra clase reservada.",
                "SOLAPE");
        }

        var estadoActiva = await _context.EstadoReservas
            .FirstOrDefaultAsync(e => e.Nombre == "Activa");

        if (estadoActiva == null)
            return ServiceResult.Fail("No existe el estado 'Activa' en la base de datos.", "ESTADO");

        reserva.EstadoReservaId = estadoActiva.Id;
        reserva.Activo = true;
        reserva.FechaBaja = null;
        reserva.FechaReserva = DateTime.Now;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Reserva reactivada correctamente.");
    }

    public async Task<bool> YaReservadaAsync(int usuarioId, int claseId)
    {
        return await _context.Reservas.AnyAsync(r =>
            r.UsuarioId == usuarioId &&
            r.ClaseId == claseId &&
            r.Activo == true);
    }

    public async Task<bool> HayPlazasAsync(int claseId)
    {
        var clase = await _context.Clases
            .Include(c => c.Reservas)
            .FirstOrDefaultAsync(c => c.Id == claseId && c.Activo == true);

        if (clase == null)
            return false;

        var capacidadMaxima = clase.CapacidadMaxima ?? 0;

        if (capacidadMaxima <= 0)
            return false;

        var reservasActivas = clase.Reservas.Count(r => r.Activo == true);

        return reservasActivas < capacidadMaxima;
    }

    public async Task<List<Reserva>> GetByClasePaginadoAsync(int claseId, int page, int pageSize)
    {
        return await _context.Reservas
            .Include(r => r.Usuario)
            .Include(r => r.Clase)
            .Include(r => r.EstadoReserva)
            .Where(r => r.ClaseId == claseId && r.Activo == true)
            .OrderByDescending(r => r.FechaReserva)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountByClaseAsync(int claseId)
    {
        return await _context.Reservas
            .Where(r => r.ClaseId == claseId && r.Activo == true)
            .CountAsync();
    }

    private IQueryable<Reserva> QueryReservasPorClase(int claseId, string? search, string? estado)
    {
        var query = _context.Reservas
            .Include(r => r.Usuario)
            .Include(r => r.Clase)
            .Include(r => r.EstadoReserva)
            .Where(r => r.ClaseId == claseId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r =>
                r.Usuario.Nombre.Contains(search) ||
                r.Usuario.Apellidos.Contains(search) ||
                r.Usuario.Email.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            query = query.Where(r => r.EstadoReserva.Nombre == estado);
        }

        return query;
    }

    public async Task<List<Reserva>> GetByClaseFiltradoAsync(
        int claseId,
        string? search,
        string? estado,
        int page,
        int pageSize)
    {
        return await QueryReservasPorClase(claseId, search, estado)
            .OrderByDescending(r => r.FechaReserva)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountByClaseFiltradoAsync(
        int claseId,
        string? search,
        string? estado)
    {
        return await QueryReservasPorClase(claseId, search, estado)
            .CountAsync();
    }

    public async Task<List<Reserva>> GetByClaseExportAsync(
        int claseId,
        string? search,
        string? estado)
    {
        return await QueryReservasPorClase(claseId, search, estado)
            .OrderByDescending(r => r.FechaReserva)
            .ToListAsync();
    }

    private IQueryable<Reserva> QueryReservas(string? search, string? estado)
    {
        var query = _context.Reservas
            .Include(r => r.Usuario)
            .Include(r => r.Clase)
            .Include(r => r.EstadoReserva)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r =>
                r.Usuario.Nombre.Contains(search) ||
                r.Usuario.Apellidos.Contains(search) ||
                r.Usuario.Email.Contains(search) ||
                r.Clase.Nombre.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            query = query.Where(r => r.EstadoReserva.Nombre == estado);
        }

        return query;
    }

    public async Task<List<Reserva>> GetFiltradasAsync(
        string? search,
        string? estado,
        int page,
        int pageSize)
    {
        return await QueryReservas(search, estado)
            .OrderByDescending(r => r.FechaReserva)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountFiltradasAsync(string? search, string? estado)
    {
        return await QueryReservas(search, estado).CountAsync();
    }

    public async Task<List<Reserva>> GetFiltradasAsync(
        string? search,
        string? estado,
        int? entrenadorId,
        int page,
        int pageSize)
    {
        return await QueryReservas(search, estado, entrenadorId)
            .OrderByDescending(r => r.FechaReserva)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountFiltradasAsync(string? search, string? estado, int? entrenadorId)
    {
        return await QueryReservas(search, estado, entrenadorId).CountAsync();
    }

    public async Task<int> CountCanceladasAsync(int? entrenadorId)
    {
        var query = _context.Reservas
            .Where(r => r.Activo == false)
            .AsQueryable();

        if (entrenadorId.HasValue)
            query = query.Where(r => r.Clase.EntrenadorId == entrenadorId.Value);

        return await query.CountAsync();
    }

    public async Task<Clase?> GetClaseConReservasAsync(int claseId)
    {
        return await _context.Clases
            .Include(c => c.Reservas)
            .FirstOrDefaultAsync(c => c.Id == claseId);
    }

    public async Task<bool> PuedeGestionarClaseAsync(int claseId, int? entrenadorId)
    {
        if (!entrenadorId.HasValue)
            return true;

        return await _context.Clases.AnyAsync(c =>
            c.Id == claseId &&
            c.EntrenadorId == entrenadorId.Value);
    }

    public async Task<bool> PuedeGestionarReservaAsync(int reservaId, int? entrenadorId)
    {
        if (!entrenadorId.HasValue)
            return true;

        return await _context.Reservas.AnyAsync(r =>
            r.Id == reservaId &&
            r.Clase.EntrenadorId == entrenadorId.Value);
    }

    private IQueryable<Reserva> QueryReservas(string? search, string? estado, int? entrenadorId)
    {
        var query = QueryReservas(search, estado);

        if (entrenadorId.HasValue)
            query = query.Where(r => r.Clase.EntrenadorId == entrenadorId.Value);

        return query;
    }

    private async Task<bool> ClienteTieneSolapeAsync(int usuarioId, Clase clase, int? reservaIdExcluir = null)
    {
        return await _context.Reservas
            .Include(r => r.Clase)
            .AnyAsync(r =>
                r.UsuarioId == usuarioId &&
                r.Activo == true &&
                (!reservaIdExcluir.HasValue || r.Id != reservaIdExcluir.Value) &&
                r.Clase.Activo == true &&
                r.Clase.Fecha == clase.Fecha &&
                clase.HoraInicio < r.Clase.HoraFin &&
                clase.HoraFin > r.Clase.HoraInicio);
    }
}
