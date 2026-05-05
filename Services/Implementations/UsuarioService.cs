using FitControlWeb.Helpers;
using FitControlWeb.Data;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FitControlWeb.Services.Implementations;

public class UsuarioService : IUsuarioService
{
    private readonly FitControlDbContext _context;

    public UsuarioService(FitControlDbContext context)
    {
        _context = context;
    }

    public async Task<List<Usuario>> GetAllAsync()
    {
        return await _context.Usuarios
            .Include(u => u.Rol)
            .Where(u => u.Activo == true)
            .ToListAsync();
    }

    public async Task<Usuario?> GetByIdAsync(int id)
    {
        return await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<Usuario?> GetByEmailAsync(string email)
    {
        return await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<ServiceResult> CreateAsync(Usuario usuario)
    {
        if (await EmailExistsAsync(usuario.Email))
            return ServiceResult.Fail("Ya existe un usuario con ese email.", "EMAIL");

        usuario.FechaRegistro = DateTime.Now;
        usuario.Activo = true;
        usuario.Bloqueado = false;
        usuario.IntentosFallidos = 0;

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Usuario creado correctamente.");
    }

    public async Task<ServiceResult> UpdateAsync(Usuario usuario)
    {
        if (await EmailExistsAsync(usuario.Email, usuario.Id))
            return ServiceResult.Fail("Ya existe otro usuario con ese email.", "EMAIL");

        _context.Usuarios.Update(usuario);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Usuario actualizado correctamente.");
    }

    public async Task<ServiceResult> SoftDeleteAsync(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);

        if (usuario == null)
            return ServiceResult.Fail("El usuario no existe.", "USUARIO");

        if (usuario.Activo != true)
            return ServiceResult.Fail("El usuario ya está dado de baja.", "USUARIO");

        usuario.Activo = false;
        usuario.FechaBaja = DateTime.Now;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Usuario dado de baja correctamente.");
    }

    public async Task<ServiceResult> ActivarAsync(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);

        if (usuario == null)
            return ServiceResult.Fail("El usuario no existe.", "USUARIO");

        if (usuario.Activo == true)
            return ServiceResult.Fail("El usuario ya está activo.", "USUARIO");

        usuario.Activo = true;
        usuario.FechaBaja = null;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Usuario reactivado correctamente.");
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null)
    {
        return await _context.Usuarios.AnyAsync(u =>
            u.Email == email &&
            (!excludeUserId.HasValue || u.Id != excludeUserId.Value));
    }


    //public async Task CreateAsync(Usuario usuario)
    //{
    //    usuario.FechaRegistro = DateTime.Now;
    //    usuario.Activo = true;
    //    usuario.Bloqueado = false;
    //    usuario.IntentosFallidos = 0;

    //    _context.Usuarios.Add(usuario);
    //    await _context.SaveChangesAsync();
    //}

    //public async Task UpdateAsync(Usuario usuario)
    //{
    //    _context.Usuarios.Update(usuario);
    //    await _context.SaveChangesAsync();
    //}

    //public async Task SoftDeleteAsync(int id)
    //{
    //    var usuario = await _context.Usuarios.FindAsync(id);

    //    if (usuario == null)
    //        return;

    //    usuario.Activo = false;
    //    usuario.FechaBaja = DateTime.Now;

    //    await _context.SaveChangesAsync();
    //}

    //public async Task ActivarAsync(int id)
    //{
    //    var usuario = await _context.Usuarios.FindAsync(id);

    //    if (usuario == null)
    //        return;

    //    usuario.Activo = true;
    //    usuario.FechaBaja = null;

    //    await _context.SaveChangesAsync();
    //}

    //public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null)
    //{
    //    return await _context.Usuarios.AnyAsync(u =>
    //        u.Email == email &&
    //        (!excludeUserId.HasValue || u.Id != excludeUserId.Value));
    //}

    private IQueryable<Usuario> QueryUsuarios(string? search, int? rolId, bool? activo)
    {
        var query = _context.Usuarios
            .Include(u => u.Rol)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                u.Nombre.Contains(search) ||
                u.Apellidos.Contains(search) ||
                u.Email.Contains(search));
        }

        if (rolId.HasValue)
            query = query.Where(u => u.RolId == rolId.Value);

        if (activo.HasValue)
            query = query.Where(u => u.Activo == activo.Value);

        return query;
    }

    public async Task<List<Usuario>> GetFiltradosAsync(
        string? search,
        int? rolId,
        bool? activo,
        int page,
        int pageSize)
    {
        return await QueryUsuarios(search, rolId, activo)
            .OrderBy(u => u.Nombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountFiltradosAsync(
        string? search,
        int? rolId,
        bool? activo)
    {
        return await QueryUsuarios(search, rolId, activo).CountAsync();
    }
}