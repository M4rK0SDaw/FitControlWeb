using FitControlWeb.Data;
using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FitControlWeb.Services.Implementations;

public class MetodoPagoService : IMetodoPagoService
{
    private readonly FitControlDbContext _context;

    public MetodoPagoService(FitControlDbContext context)
    {
        _context = context;
    }

    private IQueryable<MetodoPago> Query(string? search)
    {
        var query = _context.MetodoPagos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(m => m.Nombre.Contains(search));
        }

        return query;
    }

    public async Task<List<MetodoPago>> GetFiltradosAsync(string? search, int page, int pageSize)
    {
        return await Query(search)
            .OrderBy(m => m.Nombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountFiltradosAsync(string? search)
    {
        return await Query(search).CountAsync();
    }

    public async Task<MetodoPago?> GetByIdAsync(int id)
    {
        return await _context.MetodoPagos
            .Include(m => m.Pagos)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<bool> NombreExisteAsync(string nombre, int? excludeId = null)
    {
        return await _context.MetodoPagos.AnyAsync(m =>
            m.Nombre == nombre &&
            (!excludeId.HasValue || m.Id != excludeId.Value));
    }

    public async Task<ServiceResult> CreateAsync(MetodoPago metodoPago)
    {
        metodoPago.Nombre = metodoPago.Nombre.Trim();

        if (await NombreExisteAsync(metodoPago.Nombre))
            return ServiceResult.Fail("Ya existe un método de pago con ese nombre.", "NOMBRE_DUPLICADO");

        _context.MetodoPagos.Add(metodoPago);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Método de pago creado correctamente.");
    }

    public async Task<ServiceResult> UpdateAsync(MetodoPago metodoPago)
    {
        metodoPago.Nombre = metodoPago.Nombre.Trim();

        if (await NombreExisteAsync(metodoPago.Nombre, metodoPago.Id))
            return ServiceResult.Fail("Ya existe otro método de pago con ese nombre.", "NOMBRE_DUPLICADO");

        _context.MetodoPagos.Update(metodoPago);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Método de pago actualizado correctamente.");
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var metodo = await _context.MetodoPagos
            .Include(m => m.Pagos)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (metodo == null)
            return ServiceResult.Fail("El método de pago no existe.", "METODO_NO_EXISTE");

        if (metodo.Pagos.Any())
            return ServiceResult.Fail("No puedes eliminar este método porque tiene pagos asociados.", "METODO_CON_PAGOS");

        _context.MetodoPagos.Remove(metodo);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Método de pago eliminado correctamente.");
    }
}