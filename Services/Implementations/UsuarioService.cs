using FitControlWeb.Data;
using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels;
using FitControlWeb.ViewModels.Shared;
using FitControlWeb.ViewModels.Usuarios;
using Microsoft.AspNetCore.Mvc.Rendering;
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
            return ServiceResult.Fail("El usuario ya esta dado de baja.", "USUARIO");

        var estadoCanceladaId = await _context.EstadoReservas
            .Where(e => e.Nombre == "Cancelada")
            .Select(e => e.Id)
            .FirstOrDefaultAsync();

        var hoy = DateOnly.FromDateTime(DateTime.Today);

        var suscripcionesActivas = await _context.Suscripciones
            .Where(s => s.UsuarioId == id && s.Activa == true && s.FechaFin >= DateTime.Today)
            .ToListAsync();

        foreach (var suscripcion in suscripcionesActivas)
        {
            suscripcion.Activa = false;
        }

        if (estadoCanceladaId != 0)
        {
            var reservasFuturas = await _context.Reservas
                .Include(r => r.Clase)
                .Where(r => r.UsuarioId == id && r.Activo == true && r.Clase.Fecha >= hoy)
                .ToListAsync();

            foreach (var reserva in reservasFuturas)
            {
                reserva.Activo = false;
                reserva.EstadoReservaId = estadoCanceladaId;
                reserva.FechaBaja = DateTime.Now;
            }
        }

        usuario.Activo = false;
        usuario.FechaBaja = DateTime.Now;
        usuario.RefreshToken = null;
        usuario.RefreshTokenExpiryTime = null;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Usuario dado de baja correctamente. Se han cancelado suscripciones activas y reservas futuras.");
    }

    public async Task<ServiceResult> ActivarAsync(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);

        if (usuario == null)
            return ServiceResult.Fail("El usuario no existe.", "USUARIO");

        if (usuario.Activo == true)
            return ServiceResult.Fail("El usuario ya esta activo.", "USUARIO");

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

    public async Task<List<Usuario>> GetFiltradosAsync(string? search, int? rolId, bool? activo, int page, int pageSize)
    {
        return await QueryUsuarios(search, rolId, activo)
            .OrderBy(u => u.Nombre)
            .ThenBy(u => u.Apellidos)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountFiltradosAsync(string? search, int? rolId, bool? activo)
    {
        return await QueryUsuarios(search, rolId, activo).CountAsync();
    }

    public async Task<List<Rol>> GetRolesAsync()
    {
        return await _context.Rols
            .OrderBy(r => r.Nombre)
            .ToListAsync();
    }

    public async Task<(int TotalUsuarios, int UsuariosActivos, int UsuariosInactivos, int TotalClientes, int TotalEntrenadores, int TotalAdministradores, int NuevosEsteMes)> GetKpisAsync()
    {
        var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var totalUsuarios = await _context.Usuarios.CountAsync();
        var usuariosActivos = await _context.Usuarios.CountAsync(u => u.Activo == true);
        var usuariosInactivos = await _context.Usuarios.CountAsync(u => u.Activo != true);
        var totalClientes = await _context.Usuarios.CountAsync(u => u.Rol.Nombre == "Cliente");
        var totalEntrenadores = await _context.Usuarios.CountAsync(u => u.Rol.Nombre == "Entrenador");
        var totalAdministradores = await _context.Usuarios.CountAsync(u => u.Rol.Nombre == "Administrador");
        var nuevosEsteMes = await _context.Usuarios.CountAsync(u => u.FechaRegistro >= inicioMes);

        return (totalUsuarios, usuariosActivos, usuariosInactivos, totalClientes, totalEntrenadores, totalAdministradores, nuevosEsteMes);
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

        usuario.Nombre = model.Nombre.Trim();
        usuario.Apellidos = model.Apellidos.Trim();
        usuario.Email = model.Email.Trim();
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

    public async Task<UsuarioIndexViewModel> GetIndexViewModelAsync(string? search, int? rolId, bool? activo, int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 25 or 50 ? pageSize : 10;

        var usuarios = await GetFiltradosAsync(search, rolId, activo, page, pageSize);
        var totalItems = await CountFiltradosAsync(search, rolId, activo);
        var kpis = await GetKpisAsync();

        return new UsuarioIndexViewModel
        {
            Usuarios = usuarios.Select(MapUsuarioListItem).ToList(),
            Roles = await BuildRolesSelectListAsync(rolId),
            Search = search,
            RolId = rolId,
            Activo = activo,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
            TotalUsuarios = kpis.TotalUsuarios,
            UsuariosActivos = kpis.UsuariosActivos,
            UsuariosInactivos = kpis.UsuariosInactivos,
            TotalClientes = kpis.TotalClientes,
            TotalEntrenadores = kpis.TotalEntrenadores,
            TotalAdministradores = kpis.TotalAdministradores,
            NuevosEsteMes = kpis.NuevosEsteMes
        };
    }

    public async Task<UsuarioCreateViewModel> GetCreateViewModelAsync()
    {
        return new UsuarioCreateViewModel
        {
            Roles = await BuildRolesSelectListAsync()
        };
    }

    public async Task<UsuarioEditViewModel?> GetEditViewModelAsync(int id)
    {
        var usuario = await GetByIdAsync(id);

        if (usuario == null)
            return null;

        return new UsuarioEditViewModel
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Apellidos = usuario.Apellidos,
            Email = usuario.Email,
            Telefono = usuario.Telefono,
            RolId = usuario.RolId,
            Activo = usuario.Activo ?? true,
            Roles = await BuildRolesSelectListAsync(usuario.RolId)
        };
    }

    public async Task<FileContentViewModel> ExportCsvAsync(string? search, int? rolId, bool? activo)
    {
        var usuarios = await GetFiltradosAsync(search, rolId, activo, 1, int.MaxValue);
        var headers = new[] { "Nombre", "Apellidos", "Email", "Telefono", "Rol", "Estado" };

        return new FileContentViewModel
        {
            Content = ExportHelper.ToCsv(usuarios, headers, UsuarioExportRow),
            ContentType = "text/csv",
            FileName = "usuarios.csv"
        };
    }

    public async Task<FileContentViewModel> ExportExcelAsync(string? search, int? rolId, bool? activo)
    {
        var usuarios = await GetFiltradosAsync(search, rolId, activo, 1, int.MaxValue);
        var headers = new[] { "Nombre", "Apellidos", "Email", "Telefono", "Rol", "Estado" };

        return new FileContentViewModel
        {
            Content = ExportHelper.ToExcel(
                usuarios,
                "Usuarios",
                "Listado de usuarios",
                "Usuarios filtrados del sistema",
                GetFiltros(search, rolId, activo),
                GetResumen(usuarios),
                headers,
                u => new object[]
                {
                    u.Nombre,
                    u.Apellidos,
                    u.Email,
                    u.Telefono ?? "",
                    u.Rol?.Nombre ?? "",
                    u.Activo == true ? "Activo" : "Inactivo"
                }),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileName = "usuarios.xlsx"
        };
    }

    public async Task<FileContentViewModel> ExportPdfAsync(string? search, int? rolId, bool? activo)
    {
        var usuarios = await GetFiltradosAsync(search, rolId, activo, 1, int.MaxValue);
        var headers = new[] { "Nombre", "Email", "Telefono", "Rol", "Estado" };

        return new FileContentViewModel
        {
            Content = ExportHelper.ToPdf(
                usuarios,
                "Listado de usuarios",
                "Usuarios filtrados del sistema",
                GetFiltros(search, rolId, activo),
                GetResumen(usuarios),
                headers,
                u => new[]
                {
                    $"{u.Nombre} {u.Apellidos}",
                    u.Email,
                    u.Telefono ?? "",
                    u.Rol?.Nombre ?? "",
                    u.Activo == true ? "Activo" : "Inactivo"
                }),
            ContentType = "application/pdf",
            FileName = "usuarios.pdf"
        };
    }

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

    private async Task<List<SelectListItem>> BuildRolesSelectListAsync(int? selectedId = null)
    {
        return (await GetRolesAsync())
            .Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.Nombre,
                Selected = selectedId == r.Id
            })
            .ToList();
    }

    private static UsuarioListViewModel MapUsuarioListItem(Usuario usuario)
    {
        return new UsuarioListViewModel
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Apellidos = usuario.Apellidos,
            Email = usuario.Email,
            Rol = usuario.Rol?.Nombre ?? string.Empty,
            Activo = usuario.Activo ?? false
        };
    }

    private static string[] UsuarioExportRow(Usuario usuario)
    {
        return new[]
        {
            usuario.Nombre,
            usuario.Apellidos,
            usuario.Email,
            usuario.Telefono ?? "",
            usuario.Rol?.Nombre ?? "",
            usuario.Activo == true ? "Activo" : "Inactivo"
        };
    }

    private static string[] GetFiltros(string? search, int? rolId, bool? activo)
    {
        return new[]
        {
            $"Busqueda: {(string.IsNullOrWhiteSpace(search) ? "Sin filtro" : search)}",
            $"Rol: {(rolId.HasValue ? rolId.Value.ToString() : "Todos")}",
            $"Activo: {(activo.HasValue ? (activo.Value ? "Si" : "No") : "Todos")}"
        };
    }

    private static List<ReportSummaryItem> GetResumen(List<Usuario> usuarios)
    {
        return new()
        {
            new() { Label = "Total usuarios", Value = usuarios.Count.ToString() },
            new() { Label = "Activos", Value = usuarios.Count(u => u.Activo == true).ToString() },
            new() { Label = "Inactivos", Value = usuarios.Count(u => u.Activo != true).ToString() }
        };
    }
}
