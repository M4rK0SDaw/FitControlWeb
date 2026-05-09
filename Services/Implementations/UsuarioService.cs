using FitControlWeb.Helpers;
using FitControlWeb.Data;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FitControlWeb.Services.Implementations;

public class UsuarioService : IUsuarioService
{
    private readonly FitControlDbContext _context;
    private readonly IProfilePhotoService _profilePhotoService;

    public UsuarioService(FitControlDbContext context, IProfilePhotoService profilePhotoService)
    {
        _context = context;
        _profilePhotoService = profilePhotoService;
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

    public async Task<List<Rol>> GetRolesAsync()
    {
        return await _context.Rols
            .OrderBy(r => r.Nombre)
            .ToListAsync();
    }

    public async Task<(int TotalUsuarios, int UsuariosActivos, int UsuariosInactivos, int TotalClientes, int TotalEntrenadores)> GetKpisAsync()
    {
        var totalUsuarios = await _context.Usuarios.CountAsync();
        var usuariosActivos = await _context.Usuarios.CountAsync(u => u.Activo == true);
        var usuariosInactivos = await _context.Usuarios.CountAsync(u => u.Activo != true);
        var totalClientes = await _context.Usuarios.CountAsync(u => u.Rol.Nombre == "Cliente");
        var totalEntrenadores = await _context.Usuarios.CountAsync(u => u.Rol.Nombre == "Entrenador");

        return (totalUsuarios, usuariosActivos, usuariosInactivos, totalClientes, totalEntrenadores);
    }

    public async Task<ServiceResult<Usuario>> CreateFromViewModelAsync(UsuarioCreateViewModel model, IFormFile? foto)
    {
        var usuario = new Usuario
        {
            Nombre = model.Nombre.Trim(),
            Apellidos = model.Apellidos.Trim(),
            Email = model.Email.Trim(),
            Telefono = model.Telefono,
            RolId = model.RolId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password)
        };

        var result = await CreateAsync(usuario);

        if (!result.Success)
            return ServiceResult<Usuario>.Fail(result.Message, result.Code);

        var fotoResult = await _profilePhotoService.GuardarFotoUsuarioAsync(usuario.Id, foto);

        if (!fotoResult.Success)
            return ServiceResult<Usuario>.Fail(fotoResult.Message, fotoResult.Code);

        return ServiceResult<Usuario>.Ok(usuario, result.Message);
    }

    public async Task<ServiceResult> UpdateFromViewModelAsync(UsuarioEditViewModel model)
    {
        var usuario = await GetByIdAsync(model.Id);

        if (usuario == null)
            return ServiceResult.Fail("El usuario no existe.", "USUARIO");

        usuario.Nombre = model.Nombre;
        usuario.Apellidos = model.Apellidos;
        usuario.Email = model.Email;
        usuario.Telefono = model.Telefono;
        usuario.RolId = model.RolId;
        usuario.Activo = model.Activo;

        if (!string.IsNullOrWhiteSpace(model.NuevaPassword))
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NuevaPassword);

        var result = await UpdateAsync(usuario);

        if (!result.Success)
            return result;

        var fotoResult = await _profilePhotoService.GuardarFotoUsuarioAsync(usuario.Id, model.Foto);

        return fotoResult.Success ? result : fotoResult;
    }

    public async Task<ServiceResult> GuardarFotoAsync(int id, IFormFile? foto)
    {
        var usuario = await GetByIdAsync(id);

        if (usuario == null)
            return ServiceResult.Fail("El usuario no existe.", "USUARIO");

        if (foto == null || foto.Length == 0)
            return ServiceResult.Fail("Debes seleccionar una imagen.", "FOTO");

        return await _profilePhotoService.GuardarFotoUsuarioAsync(id, foto);
    }
}
