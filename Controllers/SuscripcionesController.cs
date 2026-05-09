using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FitControlWeb.Controllers;

[Authorize(Roles = "Administrador")]
public class SuscripcionesController : Controller
{
    private readonly ISuscripcionService _suscripcionService;

    public SuscripcionesController(ISuscripcionService suscripcionService)
    {
        _suscripcionService = suscripcionService;
    }

    public async Task<IActionResult> Index(
        string? search,
        string? estado,
        int page = 1,
        int pageSize = 10)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 25 or 50 ? pageSize : 10;

        var suscripciones = await _suscripcionService.GetFiltradasAsync(search, estado, page, pageSize);
        var totalItems = await _suscripcionService.CountFiltradasAsync(search, estado);

        ViewBag.Search = search;
        ViewBag.Estado = estado;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.TotalSuscripciones = totalItems;

        return View(suscripciones);
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
    public async Task<IActionResult> Create()
    {
        await CargarCombosAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SuscripcionCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await CargarCombosAsync();
            return View(model);
        }

        var result = await _suscripcionService.CreateFromViewModelAsync(model);

        if (!result.Success)
        {
            AddServiceError(result.Code, result.Message);
            await CargarCombosAsync();
            return View(model);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var suscripcion = await _suscripcionService.GetByIdAsync(id);

        if (suscripcion == null)
            return NotFound();

        var vm = new SuscripcionEditViewModel
        {
            Id = suscripcion.Id,
            UsuarioId = suscripcion.UsuarioId,
            TipoSuscripcionId = suscripcion.TipoSuscripcionId,
            FechaInicio = suscripcion.FechaInicio,
            FechaFin = suscripcion.FechaFin,
            Activa = suscripcion.Activa ?? true
        };

        await CargarCombosAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SuscripcionEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await CargarCombosAsync();
            return View(model);
        }

        var result = await _suscripcionService.UpdateFromViewModelAsync(model);

        if (result.Code == "SUSCRIPCION_NO_EXISTE")
            return NotFound();

        if (!result.Success)
        {
            AddServiceError(result.Code, result.Message);
            await CargarCombosAsync();
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

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportCsv(string? search, string? estado)
    {
        var suscripciones = await _suscripcionService.GetFiltradasAsync(search, estado, 1, int.MaxValue);
        var headers = new[] { "Cliente", "Email", "Tipo", "Precio", "Inicio", "Fin", "Estado" };

        var bytes = ExportHelper.ToCsv(suscripciones, headers, SuscripcionExportRow);

        return File(bytes, "text/csv", "suscripciones.csv");
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportExcel(string? search, string? estado)
    {
        var suscripciones = await _suscripcionService.GetFiltradasAsync(search, estado, 1, int.MaxValue);
        var headers = new[] { "Cliente", "Email", "Tipo", "Precio", "Inicio", "Fin", "Estado" };

        var bytes = ExportHelper.ToExcel(
            suscripciones,
            "Suscripciones",
            "Listado de suscripciones",
            "Suscripciones filtradas",
            GetFiltros(search, estado),
            GetResumen(suscripciones),
            headers,
            s => new object[]
            {
                $"{s.Usuario?.Nombre ?? ""} {s.Usuario?.Apellidos ?? ""}",
                s.Usuario?.Email ?? "",
                s.TipoSuscripcion?.Nombre ?? "",
                s.TipoSuscripcion?.Precio ?? 0,
                s.FechaInicio.ToString("dd/MM/yyyy"),
                s.FechaFin.ToString("dd/MM/yyyy"),
                s.Activa == true ? "Activa" : "Cancelada"
            });

        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "suscripciones.xlsx");
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportPdf(string? search, string? estado)
    {
        try
        {
            var suscripciones = await _suscripcionService.GetFiltradasAsync(search, estado, 1, int.MaxValue);
            var headers = new[] { "Cliente", "Tipo", "Precio", "Inicio", "Fin", "Estado" };

            var bytes = ExportHelper.ToPdf(
                suscripciones,
                "Listado de suscripciones",
                "Suscripciones filtradas",
                GetFiltros(search, estado),
                GetResumen(suscripciones),
                headers,
                s => new[]
                {
                    $"{s.Usuario?.Nombre ?? ""} {s.Usuario?.Apellidos ?? ""}",
                    s.TipoSuscripcion?.Nombre ?? "",
                    $"{s.TipoSuscripcion?.Precio.ToString("0.00") ?? "0.00"} €",
                    s.FechaInicio.ToString("dd/MM/yyyy"),
                    s.FechaFin.ToString("dd/MM/yyyy"),
                    s.Activa == true ? "Activa" : "Cancelada"
                });

            return File(bytes, "application/pdf", "suscripciones.pdf");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar PDF: {ex.Message}";
            return RedirectToAction(nameof(Index), new { search, estado });
        }
    }

    private async Task CargarCombosAsync()
    {
        var usuarios = (await _suscripcionService.GetClientesActivosAsync())
            .Select(u => new
            {
                u.Id,
                NombreCompleto = u.Nombre + " " + u.Apellidos + " - " + u.Email
            })
            .OrderBy(u => u.NombreCompleto)
            .ToList();

        var tipos = await _suscripcionService.GetTiposActivosAsync();

        ViewBag.Usuarios = new SelectList(usuarios, "Id", "NombreCompleto");
        ViewBag.TiposSuscripcion = new SelectList(tipos, "Id", "Nombre");
        ViewBag.TiposSuscripcionData = tipos;
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
                ModelState.AddModelError("", message);
                break;
        }
    }

    private static string[] SuscripcionExportRow(Suscripcion s)
    {
        return new[]
        {
            $"{s.Usuario?.Nombre ?? ""} {s.Usuario?.Apellidos ?? ""}",
            s.Usuario?.Email ?? "",
            s.TipoSuscripcion?.Nombre ?? "",
            s.TipoSuscripcion?.Precio.ToString("0.00") ?? "0.00",
            s.FechaInicio.ToString("dd/MM/yyyy"),
            s.FechaFin.ToString("dd/MM/yyyy"),
            s.Activa == true ? "Activa" : "Cancelada"
        };
    }

    private static string[] GetFiltros(string? search, string? estado)
    {
        return new[]
        {
            $"Búsqueda: {(string.IsNullOrWhiteSpace(search) ? "Sin filtro" : search)}",
            $"Estado: {(string.IsNullOrWhiteSpace(estado) ? "Todos" : estado)}"
        };
    }

    private static List<ReportSummaryItem> GetResumen(List<Suscripcion> suscripciones)
    {
        var hoy = DateTime.Today;

        return new()
        {
            new() { Label = "Total", Value = suscripciones.Count.ToString() },
            new() { Label = "Activas", Value = suscripciones.Count(s => s.Activa == true && s.FechaFin >= hoy).ToString() },
            new() { Label = "Vencidas", Value = suscripciones.Count(s => s.Activa == true && s.FechaFin < hoy).ToString() },
            new() { Label = "Canceladas", Value = suscripciones.Count(s => s.Activa != true).ToString() }
        };
    }

    private IActionResult RedirectLocalOrIndex(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }
}
