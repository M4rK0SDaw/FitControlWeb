using FitControlWeb.Helpers;
using FitControlWeb.Data;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FitControlWeb.Services.Implementations;

public class ClaseService : IClaseService
{
    private readonly FitControlDbContext _context;

    public ClaseService(FitControlDbContext context)
    {
        _context = context;
    }

    public async Task<List<Clase>> GetAllAsync()
    {
        return await _context.Clases
            .Include(c => c.Entrenador)
            .Include(c => c.Especialidad)
            .Where(c => c.Activo == true)
            .ToListAsync();
    }

    public async Task<Clase?> GetByIdAsync(int id)
    {
        return await _context.Clases
            .Include(c => c.Entrenador)
            .Include(c => c.Especialidad)
            .Include(c => c.Reservas)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<ServiceResult> CreateAsync(Clase clase)
    {
        if (clase.Fecha < DateOnly.FromDateTime(DateTime.Today))
            return ServiceResult.Fail("No puedes crear una clase en una fecha pasada.", "CLASE");

        if (clase.HoraFin <= clase.HoraInicio)
            return ServiceResult.Fail("La hora de fin debe ser posterior a la hora de inicio.", "HORARIO");

        if ((clase.CapacidadMinima ?? 0) > (clase.CapacidadMaxima ?? 0))
            return ServiceResult.Fail("La capacidad mínima no puede ser mayor que la máxima.", "CAPACIDAD");

        if (await EntrenadorTieneSolapeAsync(
            clase.EntrenadorId,
            clase.Fecha,
            clase.HoraInicio,
            clase.HoraFin))
        {
            return ServiceResult.Fail("El entrenador ya tiene una clase en ese horario.", "SOLAPE");
        }

        clase.Activo = true;

        _context.Clases.Add(clase);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Clase creada correctamente.");
    }

    public async Task<ServiceResult> UpdateAsync(Clase clase)
    {
        if (clase.Fecha < DateOnly.FromDateTime(DateTime.Today))
            return ServiceResult.Fail("No puedes mover una clase a una fecha pasada.", "CLASE");

        if (clase.HoraFin <= clase.HoraInicio)
            return ServiceResult.Fail("La hora de fin debe ser posterior a la hora de inicio.", "HORARIO");

        if ((clase.CapacidadMinima ?? 0) > (clase.CapacidadMaxima ?? 0))
            return ServiceResult.Fail("La capacidad mínima no puede ser mayor que la máxima.", "CAPACIDAD");

        if (await EntrenadorTieneSolapeAsync(
            clase.EntrenadorId,
            clase.Fecha,
            clase.HoraInicio,
            clase.HoraFin,
            clase.Id))
        {
            return ServiceResult.Fail("El entrenador ya tiene una clase en ese horario.", "SOLAPE");
        }

        _context.Clases.Update(clase);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Clase actualizada correctamente.");
    }

    public async Task<ServiceResult> SoftDeleteAsync(int id)
    {
        var clase = await _context.Clases
            .Include(c => c.Reservas)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (clase == null)
            return ServiceResult.Fail("La clase no existe.", "CLASE");

        if (clase.Activo != true)
            return ServiceResult.Fail("La clase ya está eliminada.", "CLASE");

        var ahora = DateTime.Now;
        var hoy = DateOnly.FromDateTime(ahora);
        var horaActual = TimeOnly.FromDateTime(ahora);
        var claseFinalizada = clase.Fecha < hoy || (clase.Fecha == hoy && clase.HoraFin <= horaActual);

        var estadoCanceladaId = await _context.EstadoReservas
            .Where(e => e.Nombre == "Cancelada")
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync();

        var estadoFinalizadaId = await _context.EstadoReservas
            .Where(e => e.Nombre == "Finalizada")
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync();

        var reservasActualizadas = 0;

        foreach (var reserva in clase.Reservas.Where(r => r.Activo == true))
        {
            reserva.EstadoReservaId = claseFinalizada
                ? (estadoFinalizadaId ?? reserva.EstadoReservaId)
                : (estadoCanceladaId ?? reserva.EstadoReservaId);

            reserva.Activo = false;
            reserva.FechaBaja = ahora;
            reservasActualizadas++;
        }

        clase.Activo = false;
        clase.FechaBaja = ahora;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok(
            reservasActualizadas == 1
                ? "Clase dada de baja y 1 reserva actualizada correctamente."
                : $"Clase dada de baja y {reservasActualizadas} reservas actualizadas correctamente.");
    }

    public async Task<bool> EntrenadorTieneSolapeAsync(
     int entrenadorId,
     DateOnly fecha,
     TimeOnly horaInicio,
     TimeOnly horaFin,
     int? claseIdExcluir = null)
    {
        return await _context.Clases.AnyAsync(c =>
            c.EntrenadorId == entrenadorId &&
            c.Fecha == fecha &&
            c.Activo == true &&
            (!claseIdExcluir.HasValue || c.Id != claseIdExcluir.Value) &&
            horaInicio < c.HoraFin &&
            horaFin > c.HoraInicio
        );
    }

    public async Task<List<Clase>> GetFiltradasAsync(
    string search,
    int? entrenadorId,
    int? especialidadId,
    string? estado,
    int page,
    int pageSize)
    {
        var ahora = DateTime.Now;
        var hoy = DateOnly.FromDateTime(ahora);
        var horaActual = TimeOnly.FromDateTime(ahora);

        var query = _context.Clases
            .AsNoTracking()
            .Include(c => c.Entrenador)
            .Include(c => c.Especialidad)
            .Where(c => c.Activo == true)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.Nombre.Contains(search));
        }

        if (entrenadorId.HasValue)
        {
            query = query.Where(c => c.EntrenadorId == entrenadorId.Value);
        }

        if (especialidadId.HasValue)
        {
            query = query.Where(c => c.EspecialidadId == especialidadId.Value);
        }

        query = AplicarFiltroEstado(query, estado, hoy, horaActual);

        return await query
            .OrderBy(c => c.Fecha)
            .ThenBy(c => c.HoraInicio)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountFiltradasAsync(
        string search,
        int? entrenadorId,
        int? especialidadId,
        string? estado)
    {
        var ahora = DateTime.Now;
        var hoy = DateOnly.FromDateTime(ahora);
        var horaActual = TimeOnly.FromDateTime(ahora);

        var query = _context.Clases
            .AsNoTracking()
            .Where(c => c.Activo == true)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.Nombre.Contains(search));
        }

        if (entrenadorId.HasValue)
        {
            query = query.Where(c => c.EntrenadorId == entrenadorId.Value);
        }

        if (especialidadId.HasValue)
        {
            query = query.Where(c => c.EspecialidadId == especialidadId.Value);
        }

        query = AplicarFiltroEstado(query, estado, hoy, horaActual);

        return await query.CountAsync();
    }

    public async Task<List<ClaseListViewModel>> GetListViewAsync(
        string search,
        int? entrenadorId,
        int? especialidadId,
        string? estado,
        int page,
        int pageSize,
        int? usuarioClienteId)
    {
        var clases = await GetFiltradasAsync(search, entrenadorId, especialidadId, estado, page, pageSize);
        var ahora = DateTime.Now;
        var hoy = DateOnly.FromDateTime(ahora);
        var horaActual = TimeOnly.FromDateTime(ahora);

        var clienteTieneSuscripcionActiva = false;
        var clasesReservadasCliente = new List<int>();

        if (usuarioClienteId.HasValue)
        {
            clienteTieneSuscripcionActiva = await _context.Suscripciones.AnyAsync(s =>
                s.UsuarioId == usuarioClienteId.Value &&
                s.Activa == true &&
                s.FechaFin >= DateTime.Today);

            clasesReservadasCliente = await _context.Reservas
                .Where(r => r.UsuarioId == usuarioClienteId.Value && r.Activo == true)
                .Select(r => r.ClaseId)
                .ToListAsync();
        }

        var claseIds = clases.Select(c => c.Id).ToList();

        var plazasOcupadasPorClase = await _context.Reservas
            .Where(r => claseIds.Contains(r.ClaseId) && r.Activo == true)
            .GroupBy(r => r.ClaseId)
            .Select(g => new { ClaseId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.ClaseId, x => x.Total);

        return clases.Select(c =>
        {
            var plazasOcupadas = plazasOcupadasPorClase.TryGetValue(c.Id, out var total)
                ? total
                : 0;

            var capacidadMaxima = c.CapacidadMaxima ?? 0;

            return new ClaseListViewModel
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Fecha = c.Fecha,
                HoraInicio = c.HoraInicio,
                HoraFin = c.HoraFin,
                CapacidadMaxima = capacidadMaxima,
                PlazasOcupadas = plazasOcupadas,
                Entrenador = $"{c.Entrenador.Nombre} {c.Entrenador.Apellidos}",
                Especialidad = c.Especialidad.Nombre,
                Completa = capacidadMaxima > 0 && plazasOcupadas >= capacidadMaxima,
                YaReservada = clasesReservadasCliente.Contains(c.Id),
                EsPasada = c.Fecha < hoy || (c.Fecha == hoy && c.HoraFin <= horaActual),
                ClienteTieneSuscripcionActiva = clienteTieneSuscripcionActiva
            };
        }).ToList();
    }

    public async Task<List<Usuario>> GetEntrenadoresActivosAsync()
    {
        return await _context.Usuarios
            .Include(u => u.Rol)
            .Where(u => u.Activo == true && u.Rol.Nombre == "Entrenador")
            .OrderBy(u => u.Nombre)
            .ThenBy(u => u.Apellidos)
            .ToListAsync();
    }

    public async Task<List<Especialidad>> GetEspecialidadesActivasAsync()
    {
        return await _context.Especialidades
            .Where(e => e.Activo == true)
            .OrderBy(e => e.Nombre)
            .ToListAsync();
    }

    public async Task<bool> PuedeVerClaseAsync(int claseId, int? entrenadorId)
    {
        if (!entrenadorId.HasValue)
            return true;

        return await _context.Clases.AnyAsync(c =>
            c.Id == claseId &&
            c.EntrenadorId == entrenadorId.Value);
    }

    private static IQueryable<Clase> AplicarFiltroEstado(
        IQueryable<Clase> query,
        string? estado,
        DateOnly hoy,
        TimeOnly horaActual)
    {
        if (string.IsNullOrWhiteSpace(estado) || estado == "Todas")
            return query;

        var normalizado = estado.Trim().ToLowerInvariant();

        if (normalizado == "finalizadas")
        {
            return query.Where(c =>
                c.Fecha < hoy ||
                (c.Fecha == hoy && c.HoraFin <= horaActual));
        }

        if (normalizado == "completas")
        {
            return query.Where(c =>
                c.Fecha > hoy || (c.Fecha == hoy && c.HoraFin > horaActual))
                .Where(c => c.CapacidadMaxima != null && c.CapacidadMaxima > 0)
                .Where(c => c.Reservas.Count(r => r.Activo == true) >= c.CapacidadMaxima);
        }

        if (normalizado == "disponibles")
        {
            return query.Where(c =>
                c.Fecha > hoy || (c.Fecha == hoy && c.HoraFin > horaActual))
                .Where(c =>
                    c.CapacidadMaxima == null ||
                    c.CapacidadMaxima <= 0 ||
                    c.Reservas.Count(r => r.Activo == true) < c.CapacidadMaxima);
        }

        return query;
    }

}
