using FitControlWeb.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FitControlWeb.Controllers;

[Authorize]
public class ReservasController : Controller
{
    private readonly IReservaService _reservaService;

    public ReservasController(IReservaService reservaService)
    {
        _reservaService = reservaService;
    }

    [Authorize(Roles = "Administrador,Entrenador")]
    public async Task<IActionResult> Index(string? search, string? estado, int page = 1, int pageSize = 10)
    {
        var entrenadorId = User.IsInRole("Entrenador") ? GetUsuarioId() : (int?)null;
        var vm = await _reservaService.GetIndexViewModelAsync(search, estado, page, pageSize, entrenadorId);
        return View(vm);
    }

    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> MisReservas()
    {
        var reservas = await _reservaService.GetByUsuarioAsync(GetUsuarioId()!.Value);
        return View(reservas);
    }

    [Authorize(Roles = "Cliente")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reservar(int claseId, string? returnUrl)
    {
        var result = await _reservaService.CrearAsync(GetUsuarioId()!.Value, claseId);
        TempData[result.Success ? "Success" : "Error"] = result.Message;

        if (result.Success)
            return RedirectToAction(nameof(MisReservas));

        return RedirectLocalOr(returnUrl, "Index", "Clases");
    }

    [Authorize(Roles = "Cliente")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        var reserva = await _reservaService.GetByIdAsync(id);

        if (reserva == null)
            return NotFound();

        if (reserva.UsuarioId != GetUsuarioId())
            return Forbid();

        var result = await _reservaService.CancelarAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(MisReservas));
    }

    [Authorize(Roles = "Administrador,Entrenador")]
    public async Task<IActionResult> PorClase(int claseId, string? search, string? estado, int page = 1, int pageSize = 10)
    {
        var entrenadorId = User.IsInRole("Entrenador") ? GetUsuarioId() : (int?)null;
        var result = await _reservaService.GetPorClaseViewModelAsync(claseId, search, estado, page, pageSize, entrenadorId);

        if (result.Code == "NOT_FOUND")
            return NotFound();

        if (result.Code == "FORBID")
            return Forbid();

        return View(result.Data);
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportReservasCsv(int claseId, string? search, string? estado)
    {
        var result = await _reservaService.ExportReservasCsvAsync(claseId, search, estado, null);

        if (result.Code == "FORBID")
            return Forbid();

        return File(result.Data!.Content, result.Data.ContentType, result.Data.FileName);
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportReservasExcel(int claseId, string? search, string? estado)
    {
        var result = await _reservaService.ExportReservasExcelAsync(claseId, search, estado, null);

        if (result.Code == "FORBID")
            return Forbid();

        return File(result.Data!.Content, result.Data.ContentType, result.Data.FileName);
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportReservasPdf(int claseId, string? search, string? estado)
    {
        var result = await _reservaService.ExportReservasPdfAsync(claseId, search, estado, null);

        if (result.Code == "FORBID")
            return Forbid();

        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(PorClase), new { claseId, search, estado });
        }

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    [Authorize(Roles = "Administrador,Entrenador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarReservaClase(int id, string? returnUrl)
    {
        var forbidden = await ValidarAccesoReservaAsync(id);
        if (forbidden != null) return forbidden;

        var result = await _reservaService.CancelarAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectLocalOrIndex(returnUrl);
    }

    [Authorize(Roles = "Administrador,Entrenador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReactivarReservaClase(int id, string? returnUrl)
    {
        var forbidden = await ValidarAccesoReservaAsync(id);
        if (forbidden != null) return forbidden;

        var result = await _reservaService.ReactivarAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectLocalOrIndex(returnUrl);
    }

    private async Task<IActionResult?> ValidarAccesoReservaAsync(int reservaId)
    {
        var reserva = await _reservaService.GetByIdAsync(reservaId);

        if (reserva == null)
            return NotFound();

        var entrenadorId = User.IsInRole("Entrenador") ? GetUsuarioId() : (int?)null;

        if (!await _reservaService.PuedeGestionarReservaAsync(reservaId, entrenadorId))
            return Forbid();

        return null;
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

    private IActionResult RedirectLocalOr(string? returnUrl, string action, string controller)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(action, controller);
    }
}
