using FitControlWeb.Data;
using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitControlWeb.Controllers;

[Authorize(Roles = "Administrador")]
public class UsuariosController : Controller
{
    private readonly IUsuarioService _usuarioService;
    private readonly FitControlDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public UsuariosController(
      IUsuarioService usuarioService,
      FitControlDbContext context,
      IWebHostEnvironment environment)
    {
        _usuarioService = usuarioService;
        _context = context;
        _environment = environment;
    }

    public async Task<IActionResult> Index(
        string? search,
        int? rolId,
        bool? activo,
        int page = 1,
        int pageSize = 10)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 25 or 50 ? pageSize : 10;

        var usuarios = await _usuarioService.GetFiltradosAsync(search, rolId, activo, page, pageSize);
        var totalItems = await _usuarioService.CountFiltradosAsync(search, rolId, activo);

        var vm = usuarios.Select(u => new UsuarioListViewModel
        {
            Id = u.Id,
            Nombre = u.Nombre,
            Apellidos = u.Apellidos,
            Email = u.Email,
            Rol = u.Rol.Nombre,
            Activo = u.Activo ?? false
        }).ToList();

        ViewBag.Search = search;
        ViewBag.RolId = rolId;
        ViewBag.Activo = activo;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        await CargarKpisAsync();
        await CargarRolesAsync();

        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var usuario = await _usuarioService.GetByIdAsync(id);

        if (usuario == null)
            return NotFound();

        return View(usuario);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await CargarRolesAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UsuarioCreateViewModel model)
    {
        // La foto es opcional, no debe romper el ModelState
        ModelState.Remove(nameof(model.Foto));

        // Fallback por si el binder no mete la foto en model.Foto
        var foto = model.Foto ?? Request.Form.Files.FirstOrDefault(f => f.Name == "Foto");

        if (!ModelState.IsValid)
        {
            await CargarRolesAsync();
            return View(model);
        }

        var usuario = new Usuario
        {
            Nombre = model.Nombre.Trim(),
            Apellidos = model.Apellidos.Trim(),
            Email = model.Email.Trim(),
            Telefono = model.Telefono,
            RolId = model.RolId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password)
        };

        var result = await _usuarioService.CreateAsync(usuario);

        if (!result.Success)
        {
            AddServiceError(result.Code, result.Message);
            await CargarRolesAsync();
            return View(model);
        }

        var fotoResult = await GuardarFotoUsuarioAsync(usuario.Id, foto);

        if (!fotoResult.Success)
        {
            TempData["Error"] = fotoResult.Message;
            return RedirectToAction(nameof(Details), new { id = usuario.Id });
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var usuario = await _usuarioService.GetByIdAsync(id);

        if (usuario == null)
            return NotFound();

        var vm = new UsuarioEditViewModel
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Apellidos = usuario.Apellidos,
            Email = usuario.Email,
            Telefono = usuario.Telefono,
            RolId = usuario.RolId,
            Activo = usuario.Activo ?? true
        };

        await CargarRolesAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UsuarioEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await CargarRolesAsync();
            return View(model);
        }

        var usuario = await _usuarioService.GetByIdAsync(model.Id);

        if (usuario == null)
            return NotFound();

        usuario.Nombre = model.Nombre;
        usuario.Apellidos = model.Apellidos;
        usuario.Email = model.Email;
        usuario.Telefono = model.Telefono;
        usuario.RolId = model.RolId;
        usuario.Activo = model.Activo;

        if (!string.IsNullOrWhiteSpace(model.NuevaPassword))
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NuevaPassword);

        var result = await _usuarioService.UpdateAsync(usuario);

        if (!result.Success)
        {
            AddServiceError(result.Code, result.Message);
            await CargarRolesAsync();
            return View(model);
        }

        var fotoResult = await GuardarFotoUsuarioAsync(usuario.Id, model.Foto);

        if (!fotoResult.Success)
        {
            TempData["Error"] = fotoResult.Message;
            await CargarRolesAsync();
            return View(model);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl)
    {
        var result = await _usuarioService.SoftDeleteAsync(id);

        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return !string.IsNullOrWhiteSpace(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivar(int id, string? returnUrl)
    {
        var result = await _usuarioService.ActivarAsync(id);

        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return !string.IsNullOrWhiteSpace(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    private async Task CargarRolesAsync()
    {
        ViewBag.Roles = new SelectList(
            await _context.Rols
                .OrderBy(r => r.Nombre)
                .ToListAsync(),
            "Id",
            "Nombre"
        );
    }

    private async Task CargarKpisAsync()
    {
        ViewBag.TotalUsuarios = await _context.Usuarios.CountAsync();
        ViewBag.UsuariosActivos = await _context.Usuarios.CountAsync(u => u.Activo == true);
        ViewBag.UsuariosInactivos = await _context.Usuarios.CountAsync(u => u.Activo != true);

        ViewBag.TotalClientes = await _context.Usuarios
            .CountAsync(u => u.Rol.Nombre == "Cliente");

        ViewBag.TotalEntrenadores = await _context.Usuarios
            .CountAsync(u => u.Rol.Nombre == "Entrenador");
    }

    private void AddServiceError(string? code, string message)
    {
        switch (code)
        {
            case "EMAIL_DUPLICADO":
                ModelState.AddModelError("Email", message);
                break;

            case "USUARIO":
                ModelState.AddModelError("", message);
                break;

            default:
                ModelState.AddModelError("", message);
                break;
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubirFoto(int id, IFormFile foto)
    {
        var usuario = await _usuarioService.GetByIdAsync(id);

        if (usuario == null)
            return NotFound();

        if (foto == null || foto.Length == 0)
        {
            TempData["Error"] = "Debes seleccionar una imagen.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };

        var extension = Path.GetExtension(foto.FileName ?? "").ToLowerInvariant();  // var extension = Path.GetExtension(foto.FileName).ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(extension))
        {
            TempData["Error"] = "La imagen no tiene una extensión válida.";
            return RedirectToAction(nameof(Details), new { id });
        }


        if (!extensionesPermitidas.Contains(extension))
        {
            TempData["Error"] = "Formato no válido. Usa JPG, PNG o WEBP.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (foto.Length > 5 * 1024 * 1024)
        {
            TempData["Error"] = "La imagen no puede superar los 5MB.";
            return RedirectToAction(nameof(Details), new { id });
        }

        //if (foto.Length > 2 * 1024 * 1024)
        //{
        //    TempData["Error"] = "La imagen no puede superar los 2MB.";
        //    return RedirectToAction(nameof(Details), new { id });
        //}

        var carpeta = Path.Combine(_environment.WebRootPath, "uploads", "usuarios");

        if (!Directory.Exists(carpeta))
            Directory.CreateDirectory(carpeta);

        foreach (var archivo in Directory.GetFiles(carpeta, $"usuario-{id}.*"))
        {
            System.IO.File.Delete(archivo);
        }

        var nombreArchivo = $"usuario-{id}{extension}";
        var rutaArchivo = Path.Combine(carpeta, nombreArchivo);

        using (var stream = new FileStream(rutaArchivo, FileMode.Create))
        {
            await foto.CopyToAsync(stream);
        }

        TempData["Success"] = "Foto actualizada correctamente.";
        return RedirectToAction(nameof(Details), new { id });
    }


    private async Task<ServiceResult> GuardarFotoUsuarioAsync(int usuarioId, IFormFile? foto)
    {
        if (foto == null || foto.Length == 0)
            return ServiceResult.Ok();

        var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(foto.FileName ?? "").ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(extension))
            return ServiceResult.Fail("La imagen no tiene una extensión válida.");

        if (!extensionesPermitidas.Contains(extension))
            return ServiceResult.Fail("Formato de imagen no válido. Usa JPG, PNG o WEBP.");

        // if (foto.Length > 2 * 1024 * 1024) return ServiceResult.Fail("La imagen no puede superar los 2MB.");
        if (foto.Length > 5 * 1024 * 1024)
            return ServiceResult.Fail("La imagen no puede superar los 5MB.");


        var carpeta = Path.Combine(_environment.WebRootPath, "uploads", "usuarios");

        if (!Directory.Exists(carpeta))
            Directory.CreateDirectory(carpeta);

        foreach (var archivo in Directory.GetFiles(carpeta, $"usuario-{usuarioId}.*"))
        {
            System.IO.File.Delete(archivo);
        }

        var nombreArchivo = $"usuario-{usuarioId}{extension}";
        var rutaArchivo = Path.Combine(carpeta, nombreArchivo);

        using var stream = new FileStream(rutaArchivo, FileMode.Create);
        await foto.CopyToAsync(stream);

        return ServiceResult.Ok("Foto guardada correctamente.");
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportCsv(string? search, int? rolId, bool? activo)
    {
        var usuarios = await _usuarioService.GetFiltradosAsync(search, rolId, activo, 1, int.MaxValue);

        var headers = new[]
        {
        "Nombre", "Apellidos", "Email", "Teléfono", "Rol", "Estado"
    };

        var bytes = ExportHelper.ToCsv(
            usuarios,
            headers,
            u => new[]
            {
            u.Nombre,
            u.Apellidos,
            u.Email,
            u.Telefono ?? "",
            u.Rol?.Nombre ?? "",
            u.Activo == true ? "Activo" : "Inactivo"
            });

        return File(bytes, "text/csv", "usuarios.csv");
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportExcel(string? search, int? rolId, bool? activo)
    {
        var usuarios = await _usuarioService.GetFiltradosAsync(search, rolId, activo, 1, int.MaxValue);

        var filters = new[]
        {
        $"Búsqueda: {(string.IsNullOrWhiteSpace(search) ? "Sin filtro" : search)}",
        $"RolId: {(rolId.HasValue ? rolId.Value.ToString() : "Todos")}",
        $"Activo: {(activo.HasValue ? (activo.Value ? "Sí" : "No") : "Todos")}"
    };

        var summary = new List<ReportSummaryItem>
    {
        new() { Label = "Total usuarios", Value = usuarios.Count.ToString() },
        new() { Label = "Activos", Value = usuarios.Count(u => u.Activo == true).ToString() },
        new() { Label = "Inactivos", Value = usuarios.Count(u => u.Activo != true).ToString() }
    };

        var headers = new[]
        {
        "Nombre", "Apellidos", "Email", "Teléfono", "Rol", "Estado"
    };

        var bytes = ExportHelper.ToExcel(
            usuarios,
            "Usuarios",
            "Listado de usuarios",
            "Usuarios filtrados del sistema",
            filters,
            summary,
            headers,
            u => new object[]
            {
            u.Nombre,
            u.Apellidos,
            u.Email,
            u.Telefono ?? "",
            u.Rol?.Nombre ?? "",
            u.Activo == true ? "Activo" : "Inactivo"
            });

        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "usuarios.xlsx");
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportPdf(string? search, int? rolId, bool? activo)
    {
        try
        {
            var usuarios = await _usuarioService.GetFiltradosAsync(search, rolId, activo, 1, int.MaxValue);

            var filters = new[]
            {
            $"Búsqueda: {(string.IsNullOrWhiteSpace(search) ? "Sin filtro" : search)}",
            $"RolId: {(rolId.HasValue ? rolId.Value.ToString() : "Todos")}",
            $"Activo: {(activo.HasValue ? (activo.Value ? "Sí" : "No") : "Todos")}"
        };

            var summary = new List<ReportSummaryItem>
        {
            new() { Label = "Total usuarios", Value = usuarios.Count.ToString() },
            new() { Label = "Activos", Value = usuarios.Count(u => u.Activo == true).ToString() },
            new() { Label = "Inactivos", Value = usuarios.Count(u => u.Activo != true).ToString() }
        };

            var headers = new[]
            {
            "Nombre", "Email", "Teléfono", "Rol", "Estado"
        };

            var bytes = ExportHelper.ToPdf(
                usuarios,
                "Listado de usuarios",
                "Usuarios filtrados del sistema",
                filters,
                summary,
                headers,
                u => new[]
                {
                $"{u.Nombre} {u.Apellidos}",
                u.Email,
                u.Telefono ?? "",
                u.Rol?.Nombre ?? "",
                u.Activo == true ? "Activo" : "Inactivo"
                });

            return File(bytes, "application/pdf", "usuarios.pdf");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar PDF: {ex.Message}";
            return RedirectToAction(nameof(Index), new { search, rolId, activo });
        }
    }

}