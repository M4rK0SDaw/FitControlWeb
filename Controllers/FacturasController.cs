using FitControlWeb.Helpers;
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
    public async Task<IActionResult> Index(
        string? search,
        bool? pagada,
        int page = 1,
        int pageSize = 10)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 25 or 50 ? pageSize : 10;

        var facturas = await _facturaService.GetFiltradasAsync(search, pagada, page, pageSize);
        var totalItems = await _facturaService.CountFiltradasAsync(search, pagada);

        ViewBag.Search = search;
        ViewBag.Pagada = pagada;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.TotalFacturas = totalItems;
        ViewBag.TotalPagadas = facturas.Count(f => f.Pagada == true);
        ViewBag.TotalPendientes = facturas.Count(f => f.Pagada != true);
        ViewBag.ImportePagina = facturas.Sum(f => f.Total);

        return View(facturas);
    }

    [Authorize(Roles = "Administrador,Cliente")]
    public async Task<IActionResult> Details(int id)
    {
        if (!await PuedeVerFacturaAsync(id))
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
        if (!await PuedeVerFacturaAsync(id))
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
        var facturas = await _facturaService.GetFiltradasAsync(search, pagada, 1, int.MaxValue);

        var headers = new[]
        {
            "Número", "Cliente", "Email", "Tipo", "Fecha", "Subtotal", "Impuestos", "Total", "Estado"
        };

        var bytes = ExportHelper.ToCsv(
            facturas,
            headers,
            f => new[]
            {
                f.NumeroFactura,
                $"{f.Usuario?.Nombre ?? ""} {f.Usuario?.Apellidos ?? ""}",
                f.Usuario?.Email ?? "",
                f.TipoFactura?.Nombre ?? "",
                f.FechaEmision?.ToString("dd/MM/yyyy HH:mm") ?? "",
                f.Subtotal.ToString("0.00"),
                f.Impuestos.ToString("0.00"),
                f.Total.ToString("0.00"),
                f.Pagada == true ? "Pagada" : "Pendiente"
            });

        return File(bytes, "text/csv", "facturas.csv");
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportExcel(string? search, bool? pagada)
    {
        var facturas = await _facturaService.GetFiltradasAsync(search, pagada, 1, int.MaxValue);

        var filters = new[]
        {
            $"Búsqueda: {(string.IsNullOrWhiteSpace(search) ? "Sin filtro" : search)}",
            $"Pagada: {(pagada.HasValue ? (pagada.Value ? "Sí" : "No") : "Todas")}"
        };

        var summary = new List<ReportSummaryItem>
        {
            new() { Label = "Total facturas", Value = facturas.Count.ToString() },
            new() { Label = "Pagadas", Value = facturas.Count(f => f.Pagada == true).ToString() },
            new() { Label = "Pendientes", Value = facturas.Count(f => f.Pagada != true).ToString() },
            new() { Label = "Importe total", Value = facturas.Sum(f => f.Total).ToString("0.00") + " €" }
        };

        var headers = new[]
        {
            "Número", "Cliente", "Email", "Tipo", "Fecha", "Subtotal", "Impuestos", "Total", "Estado"
        };

        var bytes = ExportHelper.ToExcel(
            facturas,
            "Facturas",
            "Listado de facturas",
            "Facturas filtradas",
            filters,
            summary,
            headers,
            f => new object[]
            {
                f.NumeroFactura,
                $"{f.Usuario?.Nombre ?? ""} {f.Usuario?.Apellidos ?? ""}",
                f.Usuario?.Email ?? "",
                f.TipoFactura?.Nombre ?? "",
                f.FechaEmision?.ToString("dd/MM/yyyy HH:mm") ?? "",
                f.Subtotal,
                f.Impuestos,
                f.Total,
                f.Pagada == true ? "Pagada" : "Pendiente"
            });

        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "facturas.xlsx");
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportPdf(string? search, bool? pagada)
    {
        try
        {
            var facturas = await _facturaService.GetFiltradasAsync(search, pagada, 1, int.MaxValue);

            var filters = new[]
            {
                $"Búsqueda: {(string.IsNullOrWhiteSpace(search) ? "Sin filtro" : search)}",
                $"Pagada: {(pagada.HasValue ? (pagada.Value ? "Sí" : "No") : "Todas")}"
            };

            var summary = new List<ReportSummaryItem>
            {
                new() { Label = "Total facturas", Value = facturas.Count.ToString() },
                new() { Label = "Pagadas", Value = facturas.Count(f => f.Pagada == true).ToString() },
                new() { Label = "Pendientes", Value = facturas.Count(f => f.Pagada != true).ToString() },
                new() { Label = "Importe total", Value = facturas.Sum(f => f.Total).ToString("0.00") + " €" }
            };

            var headers = new[] { "Número", "Cliente", "Fecha", "Total", "Estado" };

            var bytes = ExportHelper.ToPdf(
                facturas,
                "Listado de facturas",
                "Facturas filtradas",
                filters,
                summary,
                headers,
                f => new[]
                {
                    f.NumeroFactura,
                    $"{f.Usuario?.Nombre ?? ""} {f.Usuario?.Apellidos ?? ""}",
                    f.FechaEmision?.ToString("dd/MM/yyyy") ?? "",
                    $"{f.Total:0.00} €",
                    f.Pagada == true ? "Pagada" : "Pendiente"
                });

            return File(bytes, "application/pdf", "facturas.pdf");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar PDF: {ex.Message}";
            return RedirectToAction(nameof(Index), new { search, pagada });
        }
    }

    [Authorize(Roles = "Administrador,Cliente")]
    [HttpGet]
    public async Task<IActionResult> DescargarPdf(int id)
    {
        if (!await PuedeVerFacturaAsync(id))
            return Forbid();

        var factura = await _facturaService.GetByIdAsync(id);

        if (factura == null)
            return NotFound();

        var bytes = FacturaPdfHelper.GenerarFacturaPdf(factura);

        return File(bytes, "application/pdf", CrearNombreFacturaPdf(factura.NumeroFactura));
    }

    [Authorize(Roles = "Administrador,Cliente")]
    [HttpGet]
    public async Task<IActionResult> VerPdf(int id)
    {
        if (!await PuedeVerFacturaAsync(id))
            return Forbid();

        var factura = await _facturaService.GetByIdAsync(id);

        if (factura == null)
            return NotFound();

        var bytes = FacturaPdfHelper.GenerarFacturaPdf(factura);

        Response.Headers.ContentDisposition = $"inline; filename=\"{CrearNombreFacturaPdf(factura.NumeroFactura)}\"";

        return File(bytes, "application/pdf");
    }

    private async Task<bool> PuedeVerFacturaAsync(int facturaId)
    {
        var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        return await _facturaService.PuedeVerFacturaAsync(
            facturaId,
            usuarioId,
            User.IsInRole("Administrador"));
    }

    private static string CrearNombreFacturaPdf(string numeroFactura)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var numeroSeguro = new string(numeroFactura
            .Select(c => invalidChars.Contains(c) ? '-' : c)
            .ToArray());

        return $"factura-{numeroSeguro}.pdf";
    }
}
