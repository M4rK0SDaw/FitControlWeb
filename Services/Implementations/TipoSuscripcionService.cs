using FitControlWeb.Data;
using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FitControlWeb.Services.Implementations;

public class TipoSuscripcionService : ITipoSuscripcionService
{
    private readonly FitControlDbContext _context;

    public TipoSuscripcionService(FitControlDbContext context)
    {
        _context = context;
    }

    private IQueryable<TipoSuscripcion> Query(string? search, bool? activo)
    {
        var query = _context.TipoSuscripciones.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t => t.Nombre.Contains(search));
        }

        if (activo.HasValue)
        {
            query = query.Where(t => t.Activo == activo.Value);
        }

        return query;
    }

    public async Task<List<TipoSuscripcion>> GetFiltradosAsync(string? search, bool? activo, int page, int pageSize)
    {
        return await Query(search, activo)
            .OrderBy(t => t.Nombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountFiltradosAsync(string? search, bool? activo)
    {
        return await Query(search, activo).CountAsync();
    }

    public async Task<TipoSuscripcion?> GetByIdAsync(int id)
    {
        return await _context.TipoSuscripciones
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<bool> NombreExisteAsync(string nombre, int? excludeId = null)
    {
        return await _context.TipoSuscripciones.AnyAsync(t =>
            t.Nombre == nombre &&
            (!excludeId.HasValue || t.Id != excludeId.Value));
    }

    public async Task<ServiceResult> CreateAsync(TipoSuscripcion tipo)
    {
        if (await NombreExisteAsync(tipo.Nombre))
            return ServiceResult.Fail("Ya existe un tipo de suscripción con ese nombre.", "NOMBRE_DUPLICADO");

        if (tipo.Precio <= 0)
            return ServiceResult.Fail("El precio debe ser mayor que 0.", "PRECIO_INVALIDO");

        if (tipo.DuracionDias <= 0)
            return ServiceResult.Fail("La duración debe ser mayor que 0 días.", "DURACION_INVALIDA");

        tipo.Activo = true;

        _context.TipoSuscripciones.Add(tipo);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Tipo de suscripción creado correctamente.");
    }

    public async Task<ServiceResult> UpdateAsync(TipoSuscripcion tipo)
    {
        if (await NombreExisteAsync(tipo.Nombre, tipo.Id))
            return ServiceResult.Fail("Ya existe otro tipo de suscripción con ese nombre.", "NOMBRE_DUPLICADO");

        if (tipo.Precio <= 0)
            return ServiceResult.Fail("El precio debe ser mayor que 0.", "PRECIO_INVALIDO");

        if (tipo.DuracionDias <= 0)
            return ServiceResult.Fail("La duración debe ser mayor que 0 días.", "DURACION_INVALIDA");

        _context.TipoSuscripciones.Update(tipo);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Tipo de suscripción actualizado correctamente.");
    }

    public async Task<ServiceResult> DesactivarAsync(int id)
    {
        var tipo = await _context.TipoSuscripciones.FindAsync(id);

        if (tipo == null)
            return ServiceResult.Fail("El tipo de suscripción no existe.", "TIPO_NO_EXISTE");

        if (tipo.Activo != true)
            return ServiceResult.Fail("El tipo de suscripción ya está inactivo.", "TIPO_INACTIVO");

        tipo.Activo = false;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Tipo de suscripción desactivado correctamente.");
    }

    public async Task<ServiceResult> ActivarAsync(int id)
    {
        var tipo = await _context.TipoSuscripciones.FindAsync(id);

        if (tipo == null)
            return ServiceResult.Fail("El tipo de suscripción no existe.", "TIPO_NO_EXISTE");

        if (tipo.Activo == true)
            return ServiceResult.Fail("El tipo de suscripción ya está activo.", "TIPO_ACTIVO");

        tipo.Activo = true;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Tipo de suscripción activado correctamente.");
    }
}