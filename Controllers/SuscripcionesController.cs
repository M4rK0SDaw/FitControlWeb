using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitControlWeb.Controllers;

[Authorize(Roles = "Administrador")]
public class SuscripcionesController : Controller
{
    private readonly ISuscripcionService _suscripcionService;

    public SuscripcionesController(ISuscripcionService suscripcionService)
    {
        _suscripcionService = suscripcionService;
    }

    public async Task<IActionResult> Index(string? search, string? estado, int page = 1, int pageSize = 10)
    {
        var vm = await _suscripcionService.GetIndexViewModelAsync(search, estado, page, pageSize);
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var suscripcion = await _suscripcionService.GetByIdAsync(id);

        if (suscripcion == null)
            return NotFound();

        var facturaId = await _suscripcionService.GetFacturaIdAsync(id);
        ViewBag.FacturaId = facturaId;
        ViewBag.TieneFactura = facturaId.HasValue;

        return View(suscripcion);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? usuarioId = null)
    {
        return View(await _suscripcionService.GetCreateViewModelAsync(usuarioId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SuscripcionCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await ReloadCreateCombosAsync(model);
            return View(model);
        }

        var result = await _suscripcionService.CreateFromViewModelAsync(model);

        if (!result.Success)
        {
            AddServiceError(result.Code, result.Message);
            await ReloadCreateCombosAsync(model);
            return View(model);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction("Details", "Facturas", new { id = result.Data!.FacturaId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vm = await _suscripcionService.GetEditViewModelAsync(id);

        if (vm == null)
            return NotFound();

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SuscripcionEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await ReloadEditCombosAsync(model);
            return View(model);
        }

        var result = await _suscripcionService.UpdateFromViewModelAsync(model);

        if (result.Code == "SUSCRIPCION_NO_EXISTE")
            return NotFound();

        if (!result.Success)
        {
            AddServiceError(result.Code, result.Message);
            await ReloadEditCombosAsync(model);
            return View(model);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id, string? returnUrl)
    {
        var result = await _suscripcionService.CancelarAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectLocalOrIndex(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivar(int id, string? returnUrl)
    {
        var result = await _suscripcionService.ReactivarAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectLocalOrIndex(returnUrl);
    }

    public async Task<IActionResult> ExportCsv(string? search, string? estado)
    {
        var file = await _suscripcionService.ExportCsvAsync(search, estado);
        return File(file.Content, file.ContentType, file.FileName);
    }

    public async Task<IActionResult> ExportExcel(string? search, string? estado)
    {
        var file = await _suscripcionService.ExportExcelAsync(search, estado);
        return File(file.Content, file.ContentType, file.FileName);
    }

    public async Task<IActionResult> ExportPdf(string? search, string? estado)
    {
        try
        {
            var file = await _suscripcionService.ExportPdfAsync(search, estado);
            return File(file.Content, file.ContentType, file.FileName);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar PDF: {ex.Message}";
            return RedirectToAction(nameof(Index), new { search, estado });
        }
    }

    private async Task ReloadCreateCombosAsync(SuscripcionCreateViewModel model)
    {
        var viewModel = await _suscripcionService.GetCreateViewModelAsync(model.UsuarioId > 0 ? model.UsuarioId : null);
        model.Usuarios = viewModel.Usuarios;
        model.TiposSuscripcion = viewModel.TiposSuscripcion;
        model.TiposSuscripcionData = viewModel.TiposSuscripcionData;
    }

    private async Task ReloadEditCombosAsync(SuscripcionEditViewModel model)
    {
        var viewModel = await _suscripcionService.GetEditViewModelAsync(model.Id);
        model.Usuarios = viewModel?.Usuarios ?? new();
        model.TiposSuscripcion = viewModel?.TiposSuscripcion ?? new();
        model.TiposSuscripcionData = viewModel?.TiposSuscripcionData ?? new();
    }

    private void AddServiceError(string? code, string message)
    {
        switch (code)
        {
            case "SUSCRIPCION_DUPLICADA":
                ModelState.AddModelError("UsuarioId", message);
                break;
            case "TIPO_NO_VALIDO":
                ModelState.AddModelError("TipoSuscripcionId", message);
                break;
            case "FECHAS_INVALIDAS":
                ModelState.AddModelError("FechaFin", message);
                break;
            default:
                ModelState.AddModelError(string.Empty, message);
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
