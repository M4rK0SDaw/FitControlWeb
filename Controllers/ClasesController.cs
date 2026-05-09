using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 25 or 50 or 100 ? pageSize : 10;

        if (User.IsInRole("Entrenador"))
            entrenadorId = GetUsuarioId();

        if (User.IsInRole("Cliente") && string.IsNullOrWhiteSpace(estado))
            estado = "Disponibles";

        var totalItems = await _claseService.CountFiltradasAsync(
            search,
            entrenadorId,
            especialidadId,
            estado);

        var usuarioClienteId = User.IsInRole("Cliente")
            ? GetUsuarioId()
            : (int?)null;

        var vm = await _claseService.GetListViewAsync(
            search,
            entrenadorId,
            especialidadId,
            estado,
            page,
            pageSize,
            usuarioClienteId);

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.PageSize = pageSize;
        ViewBag.Search = search;
        ViewBag.EntrenadorId = entrenadorId;
        ViewBag.EspecialidadId = especialidadId;
        ViewBag.Estado = estado;

        await CargarCombosAsync();

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

        var clase = new Clase
        {
            Nombre = model.Nombre,
            Fecha = model.Fecha,
            HoraInicio = model.HoraInicio,
            HoraFin = model.HoraFin,
            CapacidadMinima = model.CapacidadMinima,
            CapacidadMaxima = model.CapacidadMaxima,
            EntrenadorId = model.EntrenadorId,
            EspecialidadId = model.EspecialidadId,
            Activo = true
        };

        var result = await _claseService.CreateAsync(clase);

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
        var clase = await _claseService.GetByIdAsync(id);

        if (clase == null)
            return NotFound();

        var vm = new ClaseEditViewModel
        {
            Id = clase.Id,
            Nombre = clase.Nombre,
            Fecha = clase.Fecha,
            HoraInicio = clase.HoraInicio,
            HoraFin = clase.HoraFin,
            CapacidadMinima = clase.CapacidadMinima ?? 1,
            CapacidadMaxima = clase.CapacidadMaxima ?? 50,
            EntrenadorId = clase.EntrenadorId,
            EspecialidadId = clase.EspecialidadId
        };

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

        var clase = await _claseService.GetByIdAsync(model.Id);

        if (clase == null)
            return NotFound();

        clase.Nombre = model.Nombre;
        clase.Fecha = model.Fecha;
        clase.HoraInicio = model.HoraInicio;
        clase.HoraFin = model.HoraFin;
        clase.CapacidadMinima = model.CapacidadMinima;
        clase.CapacidadMaxima = model.CapacidadMaxima;
        clase.EntrenadorId = model.EntrenadorId;
        clase.EspecialidadId = model.EspecialidadId;

        var result = await _claseService.UpdateAsync(clase);

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

    [Authorize(Roles = "Administrador,Entrenador")]
    public async Task<IActionResult> ExportCsv(string? search, int? entrenadorId, int? especialidadId, string? estado)
    {
        if (User.IsInRole("Entrenador"))
            entrenadorId = GetUsuarioId();

        var clases = await _claseService.GetFiltradasAsync(search ?? "", entrenadorId, especialidadId, estado, 1, int.MaxValue);

        var headers = new[] { "Clase", "Especialidad", "Fecha", "Hora inicio", "Hora fin", "Entrenador", "Capacidad máxima" };

        var bytes = ExportHelper.ToCsv(
            clases,
            headers,
            c => new[]
            {
                c.Nombre,
                c.Especialidad?.Nombre ?? "",
                c.Fecha.ToString("dd/MM/yyyy"),
                c.HoraInicio.ToString(),
                c.HoraFin.ToString(),
                $"{c.Entrenador?.Nombre ?? ""} {c.Entrenador?.Apellidos ?? ""}",
                (c.CapacidadMaxima ?? 0).ToString()
            });

        return File(bytes, "text/csv", "clases.csv");
    }

    [Authorize(Roles = "Administrador,Entrenador")]
    public async Task<IActionResult> ExportExcel(string? search, int? entrenadorId, int? especialidadId, string? estado)
    {
        if (User.IsInRole("Entrenador"))
            entrenadorId = GetUsuarioId();

        var clases = await _claseService.GetFiltradasAsync(search ?? "", entrenadorId, especialidadId, estado, 1, int.MaxValue);

        var filters = GetFiltrosExport(search, entrenadorId, especialidadId, estado);
        var summary = GetResumenClases(clases);
        var headers = new[] { "Clase", "Especialidad", "Fecha", "Hora inicio", "Hora fin", "Entrenador", "Capacidad máxima" };

        var bytes = ExportHelper.ToExcel(
            clases,
            "Clases",
            "Listado de clases",
            "Clases filtradas del gimnasio",
            filters,
            summary,
            headers,
            c => new object[]
            {
                c.Nombre,
                c.Especialidad?.Nombre ?? "",
                c.Fecha.ToString("dd/MM/yyyy"),
                c.HoraInicio.ToString(),
                c.HoraFin.ToString(),
                $"{c.Entrenador?.Nombre ?? ""} {c.Entrenador?.Apellidos ?? ""}",
                c.CapacidadMaxima ?? 0
            });

        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "clases.xlsx");
    }

    [Authorize(Roles = "Administrador,Entrenador")]
    public async Task<IActionResult> ExportPdf(string? search, int? entrenadorId, int? especialidadId, string? estado)
    {
        try
        {
            if (User.IsInRole("Entrenador"))
                entrenadorId = GetUsuarioId();

            var clases = await _claseService.GetFiltradasAsync(search ?? "", entrenadorId, especialidadId, estado, 1, int.MaxValue);
            var filters = GetFiltrosExport(search, entrenadorId, especialidadId, estado);
            var summary = GetResumenClases(clases);
            var headers = new[] { "Clase", "Especialidad", "Fecha", "Horario", "Entrenador", "Cap." };

            var bytes = ExportHelper.ToPdf(
                clases,
                "Listado de clases",
                "Clases filtradas del gimnasio",
                filters,
                summary,
                headers,
                c => new[]
                {
                    c.Nombre.Length > 35 ? c.Nombre.Substring(0, 35) + "..." : c.Nombre,
                    c.Especialidad?.Nombre ?? "",
                    c.Fecha.ToString("dd/MM/yyyy"),
                    $"{c.HoraInicio:HH\\:mm}-{c.HoraFin:HH\\:mm}",
                    $"{c.Entrenador?.Nombre ?? ""} {c.Entrenador?.Apellidos ?? ""}",
                    (c.CapacidadMaxima ?? 0).ToString()
                });

            return File(bytes, "application/pdf", "clases.pdf");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar PDF: {ex.Message}";
            return RedirectToAction(nameof(Index), new { search, entrenadorId, especialidadId, estado });
        }
    }

    private async Task CargarCombosAsync()
    {
        var entrenadores = (await _claseService.GetEntrenadoresActivosAsync())
            .Select(u => new { u.Id, NombreCompleto = u.Nombre + " " + u.Apellidos })
            .ToList();

        ViewBag.Entrenadores = new SelectList(entrenadores, "Id", "NombreCompleto");
        ViewBag.Especialidades = new SelectList(await _claseService.GetEspecialidadesActivasAsync(), "Id", "Nombre");
    }

    private void AddServiceError(string? code, string message)
    {
        switch (code)
        {
            case "Email":
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

    private static string[] GetFiltrosExport(string? search, int? entrenadorId, int? especialidadId, string? estado)
    {
        return new[]
        {
            $"Búsqueda: {(string.IsNullOrWhiteSpace(search) ? "Sin filtro" : search)}",
            $"EntrenadorId: {(entrenadorId.HasValue ? entrenadorId.Value.ToString() : "Todos")}",
            $"EspecialidadId: {(especialidadId.HasValue ? especialidadId.Value.ToString() : "Todas")}",
            $"Estado: {(string.IsNullOrWhiteSpace(estado) ? "Todos" : estado)}"
        };
    }

    private static List<ReportSummaryItem> GetResumenClases(List<Clase> clases)
    {
        return new()
        {
            new() { Label = "Total clases", Value = clases.Count.ToString() },
            new() { Label = "Clases futuras", Value = clases.Count(c => c.Fecha >= DateOnly.FromDateTime(DateTime.Today)).ToString() },
            new() { Label = "Clases pasadas", Value = clases.Count(c => c.Fecha < DateOnly.FromDateTime(DateTime.Today)).ToString() }
        };
    }

    private int GetUsuarioId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    private IActionResult RedirectLocalOrIndex(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }
}
