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
public class SuscripcionesController : Controller
{
    private readonly ISuscripcionService _suscripcionService;
    private readonly FitControlDbContext _context;

    public SuscripcionesController(ISuscripcionService suscripcionService, FitControlDbContext context)
    {
        _suscripcionService = suscripcionService;
        _context = context;
    }

    public async Task<IActionResult> Index(
        string? search,
        string? estado,
        int page = 1,
        int pageSize = 10)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 25 or 50 ? pageSize : 10;

        var suscripciones = await _suscripcionService.GetFiltradasAsync(
            search,
            estado,
            page,
            pageSize);

        var totalItems = await _suscripcionService.CountFiltradasAsync(
            search,
            estado);

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

        var factura = await _context.Facturas
            .FirstOrDefaultAsync(f =>
                f.Activo == true &&
                f.NumeroFactura.EndsWith($"-SUS-{id}"));

        ViewBag.FacturaId = factura?.Id;
        ViewBag.TieneFactura = factura != null;

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

        var tipo = await _context.TipoSuscripciones
            .FirstOrDefaultAsync(t =>
                t.Id == model.TipoSuscripcionId &&
                t.Activo == true);

        if (tipo == null)
        {
            ModelState.AddModelError(
                "TipoSuscripcionId",
                "Debes seleccionar un tipo de suscripción válido.");

            await CargarCombosAsync();
            return View(model);
        }

        var suscripcion = new Suscripcion
        {
            UsuarioId = model.UsuarioId,
            TipoSuscripcionId = model.TipoSuscripcionId,
            FechaInicio = model.FechaInicio.Date,
            FechaFin = model.FechaInicio.Date.AddDays(tipo.DuracionDias),
            Activa = true
        };

        var result = await _suscripcionService.CreateAsync(suscripcion);

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

        var suscripcion = await _suscripcionService.GetByIdAsync(model.Id);

        if (suscripcion == null)
            return NotFound();

        var tipo = await _context.TipoSuscripciones
                  .FirstOrDefaultAsync(t => t.Id == model.TipoSuscripcionId && t.Activo == true);

        if (tipo == null)
        {
            ModelState.AddModelError("TipoSuscripcionId", "Debes seleccionar un tipo de suscripción válido.");
            await CargarCombosAsync();
            return View(model);
        }


        suscripcion.UsuarioId = model.UsuarioId;
        suscripcion.TipoSuscripcionId = model.TipoSuscripcionId;
        suscripcion.FechaInicio = model.FechaInicio.Date;
        //suscripcion.FechaFin = model.FechaFin.Date;
        suscripcion.FechaFin = model.FechaInicio.Date.AddDays(tipo.DuracionDias);
        suscripcion.Activa = model.Activa;
    
        var result = await _suscripcionService.UpdateAsync(suscripcion);

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

        return !string.IsNullOrWhiteSpace(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivar(int id, string? returnUrl)
    {
        var result = await _suscripcionService.ReactivarAsync(id);

        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return !string.IsNullOrWhiteSpace(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }
   
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportCsv(string? search, string? estado)
    {
        var suscripciones = await _suscripcionService.GetFiltradasAsync(search, estado, 1, int.MaxValue);

        var headers = new[]
        {
        "Cliente", "Email", "Tipo", "Precio", "Inicio", "Fin", "Estado"
    };

        var bytes = ExportHelper.ToCsv(
            suscripciones,
            headers,
            s => new[]
            {
            $"{s.Usuario?.Nombre ?? ""} {s.Usuario?.Apellidos ?? ""}",
            s.Usuario?.Email ?? "",
            s.TipoSuscripcion?.Nombre ?? "",
            s.TipoSuscripcion?.Precio.ToString("0.00") ?? "0.00",
            s.FechaInicio.ToString("dd/MM/yyyy"),
            s.FechaFin.ToString("dd/MM/yyyy"),
            s.Activa == true ? "Activa" : "Cancelada"
            });

        return File(bytes, "text/csv", "suscripciones.csv");
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportExcel(string? search, string? estado)
    {
        var suscripciones = await _suscripcionService.GetFiltradasAsync(search, estado, 1, int.MaxValue);

        var hoy = DateTime.Today;

        var filters = new[]
        {
        $"Búsqueda: {(string.IsNullOrWhiteSpace(search) ? "Sin filtro" : search)}",
        $"Estado: {(string.IsNullOrWhiteSpace(estado) ? "Todos" : estado)}"
    };

        var summary = new List<ReportSummaryItem>
    {
        new() { Label = "Total", Value = suscripciones.Count.ToString() },
        new() { Label = "Activas", Value = suscripciones.Count(s => s.Activa == true && s.FechaFin >= hoy).ToString() },
        new() { Label = "Vencidas", Value = suscripciones.Count(s => s.Activa == true && s.FechaFin < hoy).ToString() },
        new() { Label = "Canceladas", Value = suscripciones.Count(s => s.Activa != true).ToString() }
    };

        var headers = new[]
        {
        "Cliente", "Email", "Tipo", "Precio", "Inicio", "Fin", "Estado"
    };

        var bytes = ExportHelper.ToExcel(
            suscripciones,
            "Suscripciones",
            "Listado de suscripciones",
            "Suscripciones filtradas",
            filters,
            summary,
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

        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "suscripciones.xlsx");
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportPdf(string? search, string? estado)
    {
        try
        {
            var suscripciones = await _suscripcionService.GetFiltradasAsync(search, estado, 1, int.MaxValue);

            var hoy = DateTime.Today;

            var filters = new[]
            {
            $"Búsqueda: {(string.IsNullOrWhiteSpace(search) ? "Sin filtro" : search)}",
            $"Estado: {(string.IsNullOrWhiteSpace(estado) ? "Todos" : estado)}"
        };

            var summary = new List<ReportSummaryItem>
        {
            new() { Label = "Total", Value = suscripciones.Count.ToString() },
            new() { Label = "Activas", Value = suscripciones.Count(s => s.Activa == true && s.FechaFin >= hoy).ToString() },
            new() { Label = "Vencidas", Value = suscripciones.Count(s => s.Activa == true && s.FechaFin < hoy).ToString() },
            new() { Label = "Canceladas", Value = suscripciones.Count(s => s.Activa != true).ToString() }
        };

            var headers = new[]
            {
            "Cliente", "Tipo", "Precio", "Inicio", "Fin", "Estado"
        };

            var bytes = ExportHelper.ToPdf(
                suscripciones,
                "Listado de suscripciones",
                "Suscripciones filtradas",
                filters,
                summary,
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
        var usuarios = await _context.Usuarios
            .Include(u => u.Rol)
            .Where(u =>
                u.Activo == true &&
                u.Rol.Nombre == "Cliente")
            .Select(u => new
            {
                u.Id,
                NombreCompleto = u.Nombre + " " + u.Apellidos + " - " + u.Email
            })
            .OrderBy(u => u.NombreCompleto)
            .ToListAsync();

        ViewBag.Usuarios = new SelectList(
            usuarios,
            "Id",
            "NombreCompleto");

        var tipos = await _context.TipoSuscripciones
            .Where(t => t.Activo == true)
            .OrderBy(t => t.Nombre)
            .ToListAsync();

        ViewBag.TiposSuscripcion = new SelectList(
            tipos,
            "Id",
            "Nombre");

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
}