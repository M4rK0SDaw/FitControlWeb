using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FitControlWeb.Controllers;

[Authorize]
public class ClasesController : Controller
{
    private readonly IClaseService _claseService;

    public ClasesController(IClaseService claseService)
    {
        _claseService = claseService;
    }

    public async Task<IActionResult> Index(
        string search = "",
        int? entrenadorId = null,
        int? especialidadId = null,
        string? estado = null,
        int page = 1,
        int pageSize = 10)
    {
        var vm = await _claseService.GetIndexViewModelAsync(
            search,
            entrenadorId,
            especialidadId,
            estado,
            page,
            pageSize,
            User.IsInRole("Entrenador"),
            User.IsInRole("Cliente"),
            User.Identity?.IsAuthenticated == true ? GetUsuarioId() : null);

        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var clase = await _claseService.GetByIdAsync(id);

        if (clase == null)
            return NotFound();

        if (User.IsInRole("Entrenador") && !await _claseService.PuedeVerClaseAsync(id, GetUsuarioId()))
            return Forbid();

        return View(clase);
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await CargarCombosAsync();
        return View();
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClaseCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await CargarCombosAsync();
            return View(model);
        }

        var result = await _claseService.CreateFromViewModelAsync(model);

        if (!result.Success)
        {
            AddServiceError(result.Code, result.Message);
            await CargarCombosAsync();
            return View(model);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id, string? returnUrl)
    {
        var vm = await _claseService.GetEditViewModelAsync(id);

        if (vm == null)
            return NotFound();

        ViewBag.ReturnUrl = returnUrl;
        await CargarCombosAsync();
        return View(vm);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ClaseEditViewModel model, string? returnUrl)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrl;
            await CargarCombosAsync();
            return View(model);
        }

        var result = await _claseService.UpdateFromViewModelAsync(model);

        if (result.Code == "CLASE" && result.Message.Contains("no existe", StringComparison.OrdinalIgnoreCase))
            return NotFound();

        if (!result.Success)
        {
            AddServiceError(result.Code, result.Message);
            ViewBag.ReturnUrl = returnUrl;
            await CargarCombosAsync();
            return View(model);
        }

        TempData["Success"] = result.Message;
        return RedirectLocalOrIndex(returnUrl);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl)
    {
        var result = await _claseService.SoftDeleteAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectLocalOrIndex(returnUrl);
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportCsv(string? search, int? entrenadorId, int? especialidadId, string? estado)
    {
        var file = await _claseService.ExportCsvAsync(search ?? "", entrenadorId, especialidadId, estado);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportExcel(string? search, int? entrenadorId, int? especialidadId, string? estado)
    {
        var file = await _claseService.ExportExcelAsync(search ?? "", entrenadorId, especialidadId, estado);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportPdf(string? search, int? entrenadorId, int? especialidadId, string? estado)
    {
        var result = await _claseService.ExportPdfAsync(search ?? "", entrenadorId, especialidadId, estado);

        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index), new { search, entrenadorId, especialidadId, estado });
        }

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> CalendarEvents(
        string search = "",
        int? entrenadorId = null,
        int? especialidadId = null,
        string? estado = null)
    {
        var eventos = await _claseService.GetCalendarEventsAsync(
            search,
            entrenadorId,
            especialidadId,
            estado,
            User.IsInRole("Entrenador"),
            User.IsInRole("Cliente"),
            User.Identity?.IsAuthenticated == true ? GetUsuarioId() : null);

        return Json(eventos);
    }

    private async Task CargarCombosAsync()
    {
        var entrenadores = (await _claseService.GetEntrenadoresActivosAsync())
            .Select(u => new { u.Id, NombreCompleto = u.Nombre + " " + u.Apellidos })
            .ToList();

        ViewBag.Entrenadores = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(entrenadores, "Id", "NombreCompleto");
        ViewBag.Especialidades = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(await _claseService.GetEspecialidadesActivasAsync(), "Id", "Nombre");
    }

    private void AddServiceError(string? code, string message)
    {
        switch (code)
        {
            case "USUARIO":
            case "HORARIO":
            case "CLASE":
            case "CAPACIDAD":
            case "SOLAPE":
                ModelState.AddModelError(code, message);
                break;
            default:
                ModelState.AddModelError("", message);
                break;
        }
    }

    private int? GetUsuarioId()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId)
            ? usuarioId
            : null;
    }

    private IActionResult RedirectLocalOrIndex(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }
}
