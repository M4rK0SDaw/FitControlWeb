using FitControlWeb.Data;
using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FitControlWeb.Services.Implementations;

public class EspecialidadService : IEspecialidadService
{
    private readonly FitControlDbContext _context;

    public EspecialidadService(FitControlDbContext context)
    {
        _context = context;
    }

    public async Task<List<Especialidad>> GetAllAsync()
    {
        return await _context.Especialidades
            .Where(e => e.Activo == true)
            .OrderBy(e => e.Nombre)
            .ToListAsync();
    }

    public async Task<Especialidad?> GetByIdAsync(int id)
    {
        return await _context.Especialidades
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<bool> NombreExisteAsync(string nombre, int? excludeId = null)
    {
        return await _context.Especialidades.AnyAsync(e =>
            e.Nombre == nombre &&
            (!excludeId.HasValue || e.Id != excludeId.Value));
    }

    private IQueryable<Especialidad> QueryEspecialidades(string? search, bool? activo)
    {
        var query = _context.Especialidades.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e => e.Nombre.Contains(search));
        }

        if (activo.HasValue)
        {
            query = query.Where(e => e.Activo == activo.Value);
        }

        return query;
    }

    public async Task<List<Especialidad>> GetFiltradasAsync(
        string? search,
        bool? activo,
        int page,
        int pageSize)
    {
        return await QueryEspecialidades(search, activo)
            .OrderBy(e => e.Nombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountFiltradasAsync(string? search, bool? activo)
    {
        return await QueryEspecialidades(search, activo).CountAsync();
    }

    public async Task<ServiceResult> CreateAsync(Especialidad especialidad)
    {
        if (await NombreExisteAsync(especialidad.Nombre))
            return ServiceResult.Fail("Ya existe una especialidad con ese nombre.", "NOMBRE");

        especialidad.Activo = true;

        _context.Especialidades.Add(especialidad);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Especialidad creada correctamente.");
    }

    public async Task<ServiceResult> UpdateAsync(Especialidad especialidad)
    {
        if (await NombreExisteAsync(especialidad.Nombre, especialidad.Id))
            return ServiceResult.Fail("Ya existe otra especialidad con ese nombre.", "NOMBRE");

        _context.Especialidades.Update(especialidad);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Especialidad actualizada correctamente.");
    }

    public async Task<ServiceResult> SoftDeleteAsync(int id)
    {
        var especialidad = await _context.Especialidades.FindAsync(id);

        if (especialidad == null)
            return ServiceResult.Fail("La especialidad no existe.", "ESPECIALIDAD");

        if (especialidad.Activo != true)
            return ServiceResult.Fail("La especialidad ya está dada de baja.", "ESPECIALIDAD");

        especialidad.Activo = false;
        especialidad.FechaBaja = DateTime.Now;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Especialidad dada de baja correctamente.");
    }

    public async Task<ServiceResult> ActivarAsync(int id)
    {
        var especialidad = await _context.Especialidades.FindAsync(id);

        if (especialidad == null)
            return ServiceResult.Fail("La especialidad no existe.", "ESPECIALIDAD");

        if (especialidad.Activo == true)
            return ServiceResult.Fail("La especialidad ya está activa.", "ESPECIALIDAD");

        especialidad.Activo = true;
        especialidad.FechaBaja = null;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Especialidad reactivada correctamente.");
    }
}