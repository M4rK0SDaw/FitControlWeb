using FitControlWeb.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FitControlWeb.Controllers;

[Authorize]
public class FacturasController : Controller
{
    private readonly IFacturaService _facturaService;

    public FacturasController(IFacturaService facturaService)
    {
        _facturaService = facturaService;
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Index(string? search, bool? pagada, int page = 1, int pageSize = 10)
    {
        var vm = await _facturaService.GetIndexViewModelAsync(search, pagada, page, pageSize);
        return View(vm);
    }

    [Authorize(Roles = "Administrador,Cliente")]
    public async Task<IActionResult> Details(int id)
    {
        if (!await _facturaService.PuedeVerFacturaAsync(id, GetUsuarioId(), User.IsInRole("Administrador")))
            return Forbid();

        var factura = await _facturaService.GetByIdAsync(id);

        if (factura == null)
            return NotFound();

        return View(factura);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearDesdeSuscripcion(int suscripcionId)
    {
        var result = await _facturaService.CrearDesdeSuscripcionAsync(suscripcionId);

        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction("Details", "Suscripciones", new { id = suscripcionId });
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = result.Data.Id });
    }

    [Authorize(Roles = "Administrador,Cliente")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PagarConStripe(int id)
    {
        if (!await _facturaService.PuedeVerFacturaAsync(id, GetUsuarioId(), User.IsInRole("Administrador")))
            return Forbid();

        var successUrl = Url.Action(nameof(StripeSuccess), "Facturas", null, Request.Scheme)!;
        var cancelUrl = Url.Action(nameof(StripeCancel), "Facturas", null, Request.Scheme)!;

        var result = await _facturaService.CrearCheckoutStripeAsync(id, successUrl, cancelUrl);

        if (!result.Success || string.IsNullOrWhiteSpace(result.Data))
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        return Redirect(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> StripeSuccess(int facturaId, string session_id)
    {
        var result = await _facturaService.ConfirmarPagoStripeAsync(facturaId, session_id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = facturaId });
    }

    [HttpGet]
    public IActionResult StripeCancel(int facturaId)
    {
        TempData["Error"] = "Pago cancelado en Stripe.";
        return RedirectToAction(nameof(Details), new { id = facturaId });
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportCsv(string? search, bool? pagada)
    {
        var file = await _facturaService.ExportCsvAsync(search, pagada);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportExcel(string? search, bool? pagada)
    {
        var file = await _facturaService.ExportExcelAsync(search, pagada);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportPdf(string? search, bool? pagada)
    {
        var result = await _facturaService.ExportPdfAsync(search, pagada);

        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Index), new { search, pagada });
        }

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    [Authorize(Roles = "Administrador,Cliente")]
    [HttpGet]
    public async Task<IActionResult> DescargarPdf(int id)
    {
        var result = await _facturaService.GetPdfFileAsync(id, GetUsuarioId(), User.IsInRole("Administrador"), false);

        if (result.Code == "FORBID")
            return Forbid();

        if (result.Code == "NOT_FOUND")
            return NotFound();

        return File(result.Data!.Content, result.Data.ContentType, result.Data.FileName);
    }

    [Authorize(Roles = "Administrador,Cliente")]
    [HttpGet]
    public async Task<IActionResult> VerPdf(int id)
    {
        var result = await _facturaService.GetPdfFileAsync(id, GetUsuarioId(), User.IsInRole("Administrador"), true);

        if (result.Code == "FORBID")
            return Forbid();

        if (result.Code == "NOT_FOUND")
            return NotFound();

        Response.Headers.ContentDisposition = $"inline; filename=\"{result.Data!.FileName}\"";
        return File(result.Data.Content, result.Data.ContentType);
    }

    private int GetUsuarioId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
