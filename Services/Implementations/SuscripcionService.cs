using FitControlWeb.Data;
using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;

namespace FitControlWeb.Services.Implementations;

public class SuscripcionService : ISuscripcionService
{
    private readonly FitControlDbContext _context;

    public SuscripcionService(FitControlDbContext context)
    {
        _context = context;
    }

    private IQueryable<Suscripcion> QuerySuscripciones(string? search, string? estado)
    {
        var query = _context.Suscripciones
            .Include(s => s.Usuario)
            .Include(s => s.TipoSuscripcion)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s =>
                s.Usuario.Nombre.Contains(search) ||
                s.Usuario.Apellidos.Contains(search) ||
                s.Usuario.Email.Contains(search) ||
                s.TipoSuscripcion.Nombre.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            if (estado == "Activa")
                query = query.Where(s => s.Activa == true);

            if (estado == "Cancelada")
                query = query.Where(s => s.Activa != true);

            if (estado == "Vencida")
                query = query.Where(s => s.Activa == true && s.FechaFin < DateTime.Today); //  DateOnly.FromDateTime(DateTime.Today));
        }

        return query;
    }

    public async Task<List<Suscripcion>> GetFiltradasAsync(
        string? search,
        string? estado,
        int page,
        int pageSize)
    {
        return await QuerySuscripciones(search, estado)
            .OrderByDescending(s => s.FechaInicio)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountFiltradasAsync(string? search, string? estado)
    {
        return await QuerySuscripciones(search, estado).CountAsync();
    }

    public async Task<Suscripcion?> GetByIdAsync(int id)
    {
        return await _context.Suscripciones
                        .Include(s => s.Usuario)
                        .Include(s => s.TipoSuscripcion)
                        .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<bool> UsuarioTieneSuscripcionActivaAsync(int usuarioId, int? excludeId = null)
    {
        // var hoy = DateOnly.FromDateTime(DateTime.Today);
        var hoy = DateTime.Today;

        return await _context.Suscripciones.AnyAsync(s =>
            s.UsuarioId == usuarioId &&
            s.Activa == true &&
            s.FechaFin >= hoy &&
            (!excludeId.HasValue || s.Id != excludeId.Value));
    }

    public async Task<ServiceResult> CreateAsync(Suscripcion suscripcion)
    {
        if (suscripcion.FechaFin < suscripcion.FechaInicio)
            return ServiceResult.Fail("La fecha de fin no puede ser anterior a la fecha de inicio.", "FECHAS_INVALIDAS");

        if (await UsuarioTieneSuscripcionActivaAsync(suscripcion.UsuarioId))
            return ServiceResult.Fail("El usuario ya tiene una suscripción activa.", "SUSCRIPCION_DUPLICADA");

        suscripcion.Activa = true;

        _context.Suscripciones.Add(suscripcion);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Suscripción creada correctamente.");
    }

    public async Task<ServiceResult> UpdateAsync(Suscripcion suscripcion)
    {
        if (suscripcion.FechaFin < suscripcion.FechaInicio)
            return ServiceResult.Fail("La fecha de fin no puede ser anterior a la fecha de inicio.", "FECHAS_INVALIDAS");

        if (suscripcion.Activa == true &&
            await UsuarioTieneSuscripcionActivaAsync(suscripcion.UsuarioId, suscripcion.Id))
        {
            return ServiceResult.Fail("El usuario ya tiene otra suscripción activa.", "SUSCRIPCION_DUPLICADA");
        }

        _context.Suscripciones.Update(suscripcion);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Suscripción actualizada correctamente.");
    }

    public async Task<ServiceResult> CancelarAsync(int id)
    {
        var suscripcion = await _context.Suscripciones.FindAsync(id);

        if (suscripcion.FechaFin < DateTime.Today)
        {            
            return ServiceResult.Fail("La suscripción vencida no puede ser cancelada.", "SUSCRIPCION_NO_EXISTE");
        }


        if (suscripcion == null)
            return ServiceResult.Fail("La suscripción no existe.", "SUSCRIPCION_NO_EXISTE");

        if (suscripcion.Activa != true)
            return ServiceResult.Fail("La suscripción ya está cancelada.", "SUSCRIPCION_CANCELADA");

        suscripcion.Activa = false;
        //  suscripcion.FechaFin = DateTime.Now;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Suscripción cancelada correctamente.");
    }

    public async Task<ServiceResult> ReactivarAsync(int id)
    {
        var suscripcion = await _context.Suscripciones.FindAsync(id);

        if (suscripcion == null)
            return ServiceResult.Fail("La suscripción no existe.", "SUSCRIPCION_NO_EXISTE");

        if (suscripcion.Activa == true)
            return ServiceResult.Fail("La suscripción ya está activa.", "SUSCRIPCION_ACTIVA");

        if (suscripcion.FechaFin < DateTime.Today)
            return ServiceResult.Fail("No puedes reactivar una suscripción vencida.", "SUSCRIPCION_VENCIDA");

        if (await UsuarioTieneSuscripcionActivaAsync(suscripcion.UsuarioId, suscripcion.Id))
            return ServiceResult.Fail("El usuario ya tiene otra suscripción activa.", "SUSCRIPCION_DUPLICADA");

        suscripcion.Activa = true;
        // suscripcion.FechaFin = DateTime.MinValue;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Suscripción reactivada correctamente.");
    }
}