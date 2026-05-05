using FitControlWeb.Helpers;
using FitControlWeb.Data;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
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
        var clase = await _context.Clases.FindAsync(id);

        if (clase == null)
            return ServiceResult.Fail("La clase no existe.", "CLASE");

        if (clase.Activo != true)
            return ServiceResult.Fail("La clase ya está eliminada.", "CLASE");

        clase.Activo = false;
        clase.FechaBaja = DateTime.Now;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Clase eliminada correctamente.");
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
    int page,
    int pageSize)
    {
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
        int? especialidadId)
    {
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

        return await query.CountAsync();
    }

}