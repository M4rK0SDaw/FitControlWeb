using FitControlWeb.Helpers;
using System.Security.Claims;
using FitControlWeb.Data;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitControlWeb.Controllers;

[Authorize]
public class ClasesController : Controller
{
    private readonly IClaseService _claseService;
    private readonly FitControlDbContext _context;

    public ClasesController(IClaseService claseService, FitControlDbContext context)
    {
        _claseService = claseService;
        _context = context;
    }

    #region listar
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

        var hoy = DateOnly.FromDateTime(DateTime.Today);

        int? usuarioId = null;
        bool clienteTieneSuscripcionActiva = false;
        List<int> clasesReservadasCliente = new();

        if (User.IsInRole("Entrenador"))
        {
            entrenadorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        if (User.IsInRole("Cliente"))
        {
            usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (string.IsNullOrWhiteSpace(estado))
                estado = "disponibles";

            clienteTieneSuscripcionActiva = await _context.Suscripciones.AnyAsync(s =>
                s.UsuarioId == usuarioId.Value &&
                s.Activa == true &&
                s.FechaFin >= DateTime.Today);

            clasesReservadasCliente = await _context.Reservas
                .Where(r =>
                    r.UsuarioId == usuarioId.Value &&
                    r.Activo == true)
                .Select(r => r.ClaseId)
                .ToListAsync();
        }
        else
        {
            if (string.IsNullOrWhiteSpace(estado))
                estado = "todas";
        }
        if (User.IsInRole("Entrenador"))
        {
            entrenadorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        var clases = await _claseService.GetFiltradasAsync(
            search,
            entrenadorId,
            especialidadId,
            estado,
            usuarioId,
            page,
            pageSize);

        var totalItems = await _claseService.CountFiltradasAsync(
            search,
            entrenadorId,
            especialidadId,
            estado,
            usuarioId);

        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                
        if (User.IsInRole("Cliente"))
        {
            // usuarioId = int.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!);
            usuarioId = int.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!);
            clienteTieneSuscripcionActiva = await _context.Suscripciones.AnyAsync(s =>
                s.UsuarioId == usuarioId.Value &&
                s.Activa == true &&
                s.FechaFin >= DateTime.Today);

            clasesReservadasCliente = await _context.Reservas
                .Where(r =>
                    r.UsuarioId == usuarioId.Value &&
                    r.Activo == true)
                .Select(r => r.ClaseId)
                .ToListAsync();
        }

        var claseIds = clases.Select(c => c.Id).ToList();

        var plazasOcupadasPorClase = await _context.Reservas
            .Where(r =>
                claseIds.Contains(r.ClaseId) &&
                r.Activo == true)
            .GroupBy(r => r.ClaseId)
            .Select(g => new
            {
                ClaseId = g.Key,
                Total = g.Count()
            })
            .ToDictionaryAsync(x => x.ClaseId, x => x.Total);

        var vm = clases.Select(c =>
        {
            var plazasOcupadas = plazasOcupadasPorClase.ContainsKey(c.Id)
                ? plazasOcupadasPorClase[c.Id]
                : 0;

            var capacidadMaxima = c.CapacidadMaxima ?? 0;

            return new ClaseListViewModel
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Fecha = c.Fecha,
                HoraInicio = c.HoraInicio,
                HoraFin = c.HoraFin,
                CapacidadMaxima = capacidadMaxima,
                PlazasOcupadas = plazasOcupadas,
                Entrenador = $"{c.Entrenador.Nombre} {c.Entrenador.Apellidos}",
                Especialidad = c.Especialidad.Nombre,

                Completa = capacidadMaxima > 0 && plazasOcupadas >= capacidadMaxima,
                YaReservada = clasesReservadasCliente.Contains(c.Id),
                EsPasada = c.Fecha < hoy,
                ClienteTieneSuscripcionActiva = clienteTieneSuscripcionActiva
            };
        }).ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.PageSize = pageSize;
        ViewBag.Search = search;
        ViewBag.EntrenadorId = entrenadorId;
        ViewBag.EspecialidadId = especialidadId;
        ViewBag.Estado = estado;
        await CargarCombosAsync();

        return View(vm);
    }
    #endregion

    public async Task<IActionResult> Details(int id)
    {

        var clase = await _claseService.GetByIdAsync(id);

        if (clase == null)
            return NotFound();

        if (User.IsInRole("Entrenador"))
        {
            int entrenadorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (clase.EntrenadorId != entrenadorId)
                return Forbid();
        }

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
    public async Task<IActionResult> Edit(int id)
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

        await CargarCombosAsync();
        return View(vm);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ClaseEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
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
            await CargarCombosAsync();
            return View(model);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl)
    {
        var result = await _claseService.SoftDeleteAsync(id);

        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return !string.IsNullOrWhiteSpace(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    private async Task CargarCombosAsync()
    {
        var entrenadores = await _context.Usuarios
            .Include(u => u.Rol)
            .Where(u => u.Activo == true && u.Rol.Nombre == "Entrenador")
            .Select(u => new
            {
                u.Id,
                NombreCompleto = u.Nombre + " " + u.Apellidos
            })
            .ToListAsync();

        ViewBag.Entrenadores = new SelectList(entrenadores, "Id", "NombreCompleto");

        ViewBag.Especialidades = new SelectList(
            await _context.Especialidades
                .Where(e => e.Activo == true)
                .ToListAsync(),
            "Id",
            "Nombre"
        );
    }

    private void AddServiceError(string? code, string message)
    {

        switch (code)
        {
            case "Email":
                ModelState.AddModelError(code, message);
                break;
            case "USUARIO":
                ModelState.AddModelError(code, message);
                break;
            case "HORARIO":
                ModelState.AddModelError(code, message);
                break;
            case "CLASE":
                ModelState.AddModelError(code, message);
                break;
            case "CAPACIDAD":
                ModelState.AddModelError(code, message);
                break;
            case "SOLAPE":
                ModelState.AddModelError(code, message);
                break;
            default:
                ModelState.AddModelError("", message);
                break;
        }
    }

    [Authorize(Roles = "Administrador,Entrenador")]
    public async Task<IActionResult> ExportCsv(string? search, int? entrenadorId, int? especialidadId)
    {
        if (User.IsInRole("Entrenador"))
        {
            entrenadorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        var clases = await _claseService.GetFiltradasAsync(
            search ?? "",
            entrenadorId,
            especialidadId,
            estado: "todas",
            clienteId: null,
            page: 1,
            pageSize: int.MaxValue);

        var headers = new[]
        {
        "Clase",
        "Especialidad",
        "Fecha",
        "Hora inicio",
        "Hora fin",
        "Entrenador",
        "Capacidad máxima"
    };

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
    public async Task<IActionResult> ExportExcel(string? search, int? entrenadorId, int? especialidadId)
    {
        if (User.IsInRole("Entrenador"))
        {
            entrenadorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        var clases = await _claseService.GetFiltradasAsync(
            search ?? "",
            entrenadorId,
            especialidadId,
            estado: "todas",
            clienteId: null,
            page: 1,
            pageSize: int.MaxValue);

        var filters = new[]
        {
        $"Búsqueda: {(string.IsNullOrWhiteSpace(search) ? "Sin filtro" : search)}",
        $"EntrenadorId: {(entrenadorId.HasValue ? entrenadorId.Value.ToString() : "Todos")}",
        $"EspecialidadId: {(especialidadId.HasValue ? especialidadId.Value.ToString() : "Todas")}"
    };

        var summary = new List<ReportSummaryItem>
    {
        new() { Label = "Total clases", Value = clases.Count.ToString() },
        new() { Label = "Clases futuras", Value = clases.Count(c => c.Fecha >= DateOnly.FromDateTime(DateTime.Today)).ToString() },
        new() { Label = "Clases pasadas", Value = clases.Count(c => c.Fecha < DateOnly.FromDateTime(DateTime.Today)).ToString() }
    };

        var headers = new[]
        {
        "Clase",
        "Especialidad",
        "Fecha",
        "Hora inicio",
        "Hora fin",
        "Entrenador",
        "Capacidad máxima"
    };

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

        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "clases.xlsx");
    }

    [Authorize(Roles = "Administrador,Entrenador")]
    public async Task<IActionResult> ExportPdf(string? search, int? entrenadorId, int? especialidadId)
    {
        try
        {
            if (User.IsInRole("Entrenador"))
            {
                entrenadorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            }

            var clases = await _claseService.GetFiltradasAsync(
                search ?? "",
                entrenadorId,
                especialidadId,
                estado: "todas",
                clienteId: null,
                page: 1,
                pageSize: int.MaxValue);

            var filters = new[]
            {
            $"Búsqueda: {(string.IsNullOrWhiteSpace(search) ? "Sin filtro" : search)}",
            $"EntrenadorId: {(entrenadorId.HasValue ? entrenadorId.Value.ToString() : "Todos")}",
            $"EspecialidadId: {(especialidadId.HasValue ? especialidadId.Value.ToString() : "Todas")}"
        };

            var summary = new List<ReportSummaryItem>
        {
            new() { Label = "Total clases", Value = clases.Count.ToString() },
            new() { Label = "Clases futuras", Value = clases.Count(c => c.Fecha >= DateOnly.FromDateTime(DateTime.Today)).ToString() },
            new() { Label = "Clases pasadas", Value = clases.Count(c => c.Fecha < DateOnly.FromDateTime(DateTime.Today)).ToString() }
        };

            var headers = new[]
             {
                "Clase",
                "Especialidad",
                "Fecha",
                "Horario",
                "Entrenador",
                "Cap."
            };

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
            return RedirectToAction(nameof(Index), new { search, entrenadorId, especialidadId });
        }
    }

}