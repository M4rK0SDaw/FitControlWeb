using FitControlWeb.Helpers;
using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FitControlWeb.Controllers;

[Authorize(Roles = "Administrador")]
public class UsuariosController : Controller
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
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
        ModelState.Remove(nameof(model.Foto));

        if (!ModelState.IsValid)
        {
            await CargarRolesAsync();
            return View(model);
        }

        var foto = model.Foto ?? Request.Form.Files.FirstOrDefault(f => f.Name == "Foto");
        var result = await _usuarioService.CreateFromViewModelAsync(model, foto);

        if (!result.Success)
        {
            AddServiceError(result.Code, result.Message);
            await CargarRolesAsync();
            return View(model);
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

        var result = await _usuarioService.UpdateFromViewModelAsync(model);

        if (!result.Success)
        {
            AddServiceError(result.Code, result.Message);
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

        return RedirectLocalOrIndex(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivar(int id, string? returnUrl)
    {
        var result = await _usuarioService.ActivarAsync(id);

        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return RedirectLocalOrIndex(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubirFoto(int id, IFormFile foto)
    {
        var result = await _usuarioService.GuardarFotoAsync(id, foto);

        TempData[result.Success ? "Success" : "Error"] = result.Message;

        if (result.Code == "USUARIO")
            return NotFound();

        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportCsv(string? search, int? rolId, bool? activo)
    {
        var usuarios = await _usuarioService.GetFiltradosAsync(search, rolId, activo, 1, int.MaxValue);

        var headers = new[] { "Nombre", "Apellidos", "Email", "Teléfono", "Rol", "Estado" };

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

        var headers = new[] { "Nombre", "Apellidos", "Email", "Teléfono", "Rol", "Estado" };

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

            var headers = new[] { "Nombre", "Email", "Teléfono", "Rol", "Estado" };

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

    private async Task CargarRolesAsync()
    {
        ViewBag.Roles = new SelectList(await _usuarioService.GetRolesAsync(), "Id", "Nombre");
    }

    private async Task CargarKpisAsync()
    {
        var kpis = await _usuarioService.GetKpisAsync();

        ViewBag.TotalUsuarios = kpis.TotalUsuarios;
        ViewBag.UsuariosActivos = kpis.UsuariosActivos;
        ViewBag.UsuariosInactivos = kpis.UsuariosInactivos;
        ViewBag.TotalClientes = kpis.TotalClientes;
        ViewBag.TotalEntrenadores = kpis.TotalEntrenadores;
    }

    private void AddServiceError(string? code, string message)
    {
        switch (code)
        {
            case "EMAIL":
            case "EMAIL_DUPLICADO":
                ModelState.AddModelError("Email", message);
                break;

            default:
                ModelState.AddModelError("", message);
                break;
        }
    }

    private IActionResult RedirectLocalOrIndex(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }
}
