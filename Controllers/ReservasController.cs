using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf.Canvas.Wmf;
using FitControlWeb.Data;
using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitControlWeb.Controllers;

[Authorize]
public class ReservasController : Controller
{
    private readonly IReservaService _reservaService;
    private readonly FitControlDbContext _context;

    public ReservasController(IReservaService reservaService, FitControlDbContext context)
    {
        _reservaService = reservaService;
        _context = context;
    }
    
    [Authorize(Roles = "Administrador,Entrenador")]
    public async Task<IActionResult> Index(
    string? search,
    string? estado,
    int page = 1,
    int pageSize = 10)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 25 or 50 ? pageSize : 10;

        var query = _context.Reservas
            .Include(r => r.Usuario)
            .Include(r => r.Clase)
                .ThenInclude(c => c.Entrenador)
            .Include(r => r.EstadoReserva)
            .AsQueryable();

        if (User.IsInRole("Entrenador"))
        {
            int entrenadorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            query = query.Where(r => r.Clase.EntrenadorId == entrenadorId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r =>
                r.Usuario.Nombre.Contains(search) ||
                r.Usuario.Apellidos.Contains(search) ||
                r.Usuario.Email.Contains(search) ||
                r.Clase.Nombre.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            query = query.Where(r => r.EstadoReserva.Nombre == estado);
        }

        var totalItems = await query.CountAsync();

        var reservas = await query
            .OrderByDescending(r => r.FechaReserva)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Estado = estado;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.TotalReservas = totalItems;

        if (User.IsInRole("Entrenador"))
        {
            int entrenadorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            ViewBag.PlazasCanceladas = await _context.Reservas
                .CountAsync(r =>
                    r.Activo == false &&
                    r.Clase.EntrenadorId == entrenadorId);
        }
        else
        {
            ViewBag.PlazasCanceladas = await _context.Reservas
                .CountAsync(r => r.Activo == false);
        }

        return View(reservas);
    }


    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> MisReservas()
    {
        int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var reservas = await _reservaService.GetByUsuarioAsync(usuarioId);

        return View(reservas);
    }
    
    [Authorize(Roles = "Cliente")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reservar(int claseId, string? returnUrl)
    {
        int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _reservaService.CrearAsync(usuarioId, claseId);

        TempData[result.Success ? "Success" : "Error"] = result.Message;

        if (result.Success)
            return RedirectToAction(nameof(MisReservas));

        return !string.IsNullOrWhiteSpace(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "Clases");
    }


    [Authorize(Roles = "Cliente")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var reserva = await _reservaService.GetByIdAsync(id);

        if (reserva == null)
            return NotFound();

        if (reserva.UsuarioId != usuarioId)
            return Forbid();

        var result = await _reservaService.CancelarAsync(id);

        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return RedirectToAction(nameof(MisReservas));
    }

    [Authorize(Roles = "Administrador,Entrenador")]
    public async Task<IActionResult> PorClase(
        int claseId,
        string? search,
        string? estado,
        int page = 1,
        int pageSize = 10)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 25 or 50 ? pageSize : 10;

        var clase = await _context.Clases
            .Include(c => c.Reservas)
            .FirstOrDefaultAsync(c => c.Id == claseId);

        if (clase == null)
            return NotFound();

        if (User.IsInRole("Entrenador"))
        {
            int entrenadorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (clase.EntrenadorId != entrenadorId)
                return Forbid();
        }
        var reservas = await _reservaService.GetByClaseFiltradoAsync(
            claseId,
            search,
            estado,
            page,
            pageSize);

        var totalItems = await _reservaService.CountByClaseFiltradoAsync(
            claseId,
            search,
            estado);

        ViewBag.ClaseId = claseId;
        ViewBag.Search = search;
        ViewBag.Estado = estado;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.TotalReservas = totalItems;

        ViewBag.ClaseNombre = clase.Nombre;
        ViewBag.ClaseFecha = clase.Fecha.ToDateTime(TimeOnly.MinValue);
        ViewBag.ClaseHoraInicio = clase.HoraInicio;
        ViewBag.ClaseHoraFin = clase.HoraFin;
        ViewBag.CapacidadMaxima = clase.CapacidadMaxima ?? 0;
        ViewBag.PlazasOcupadas = clase.Reservas.Count(r => r.Activo == true);
        ViewBag.PlazasCanceladas = clase.Reservas.Count(r => r.Activo == false);

        return View(reservas);
    }

    [Authorize(Roles = "Administrador,Entrenador")]
    public async Task<IActionResult> ExportReservasCsv(int claseId, string? search, string? estado)
    {
        var reservas = await _reservaService.GetByClaseExportAsync(claseId, search, estado);

        var headers = new[]
        {
            "Cliente", "Email", "Clase", "Fecha reserva", "Estado"
        };

        var bytes = ExportHelper.ToCsv(
            reservas,
            headers,
            r => new[]
            {
                $"{r.Usuario?.Nombre ?? ""} {r.Usuario?.Apellidos ?? ""}",
                r.Usuario?.Email ?? "",
                r.Clase?.Nombre ?? "",
                r.FechaReserva?.ToString("dd/MM/yyyy HH:mm") ?? "",
                r.EstadoReserva?.Nombre ?? ""
            });

        return File(bytes, "text/csv", "reservas-clase.csv");
    }

    [Authorize(Roles = "Administrador,Entrenador")]
    public async Task<IActionResult> ExportReservasExcel(int claseId, string? search, string? estado)
    {
        var reservas = await _reservaService.GetByClaseExportAsync(claseId, search, estado);

        var primera = reservas.FirstOrDefault();

        var subtitle = primera?.Clase != null
            ? $"{primera.Clase.Nombre} - {primera.Clase.Fecha:dd/MM/yyyy} - {primera.Clase.HoraInicio} a {primera.Clase.HoraFin}"
            : "Reservas de la clase";

        var filters = new[]
        {
            $"Búsqueda: {(string.IsNullOrWhiteSpace(search) ? "Sin filtro" : search)}",
            $"Estado: {(string.IsNullOrWhiteSpace(estado) ? "Todos" : estado)}"
        };

        var summary = new List<ReportSummaryItem>
        {
            new() { Label = "Total", Value = reservas.Count.ToString() },
            new() { Label = "Activas", Value = reservas.Count(r => r.Activo == true).ToString() },
            new() { Label = "Canceladas", Value = reservas.Count(r => r.Activo != true).ToString() }
        };

        var headers = new[]
        {
            "Cliente",
            "Email",
            "Clase",
            "Fecha reserva",
            "Estado"
        };

        var bytes = ExportHelper.ToExcel(
            reservas,
            "Reservas",
            "Reservas por clase",
            subtitle,
            filters,
            summary,
            headers,
            r => new object[]
            {
                $"{r.Usuario?.Nombre ?? ""} {r.Usuario?.Apellidos ?? ""}",
                r.Usuario?.Email ?? "",
                r.Clase?.Nombre ?? "",
                r.FechaReserva?.ToString("dd/MM/yyyy HH:mm") ?? "",
                r.EstadoReserva?.Nombre ?? ""
            });

        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "reservas-clase.xlsx");
    }

    [Authorize(Roles = "Administrador,Entrenador")]
    public async Task<IActionResult> ExportReservasPdf(int claseId, string? search, string? estado)
    {
        try
        {
            var reservas = await _reservaService.GetByClaseExportAsync(claseId, search, estado);

            var headers = new[] { "Cliente", "Email", "Clase", "Fecha reserva", "Estado" };

            var filters = new[]
            {
                $"Búsqueda: {(string.IsNullOrWhiteSpace(search) ? "Sin filtro" : search)}",
                $"Estado: {(string.IsNullOrWhiteSpace(estado) ? "Todos" : estado)}"
            };

            var summary = new List<ReportSummaryItem>
            {
                new() { Label = "Total", Value = reservas.Count.ToString() },
                new() { Label = "Activas", Value = reservas.Count(r => r.Activo == true).ToString() },
                new() { Label = "Canceladas", Value = reservas.Count(r => r.Activo != true).ToString() }
            };

            var bytes = ExportHelper.ToPdf(
                reservas,
                "Reservas por clase",
                "Listado de reservas filtradas",
                filters,
                summary,
                headers,
                r => new[]
                {
                    $"{r.Usuario?.Nombre ?? ""} {r.Usuario?.Apellidos ?? ""}",
                    r.Usuario?.Email ?? "",
                    r.Clase?.Nombre ?? "",
                    r.FechaReserva?.ToString("dd/MM/yyyy HH:mm") ?? "",
                    r.EstadoReserva?.Nombre ?? ""
                });

            return File(bytes, "application/pdf", "reservas-clase.pdf");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar PDF: {ex.Message}";
            return RedirectToAction(nameof(PorClase), new { claseId, search, estado });
        }
    }


    [Authorize(Roles = "Administrador,Entrenador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarReservaClase(int id, string? returnUrl)
    {
        var reserva = await _context.Reservas
            .Include(r => r.Clase)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reserva == null)
            return NotFound();

        if (User.IsInRole("Entrenador"))
        {
            int entrenadorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (reserva.Clase.EntrenadorId != entrenadorId)
                return Forbid();
        }

        var result = await _reservaService.CancelarAsync(id);

        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return !string.IsNullOrWhiteSpace(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador,Entrenador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReactivarReservaClase(int id, string? returnUrl)
    {
        var reserva = await _context.Reservas
            .Include(r => r.Clase)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reserva == null)
            return NotFound();

        if (User.IsInRole("Entrenador"))
        {
            int entrenadorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (reserva.Clase.EntrenadorId != entrenadorId)
                return Forbid();
        }

        var result = await _reservaService.ReactivarAsync(id);

        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return !string.IsNullOrWhiteSpace(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }
    private void AddServiceError(string? code,
                                 string message)
    {
        switch (code)
        {
            case "CLASE":
                ModelState.AddModelError(code, message);
                break;
            case "RESERVA":
                ModelState.AddModelError(code, message);
                break;
            case "ESTADO":
                ModelState.AddModelError(code, message);
                break;
            case "PLAZAS":
                ModelState.AddModelError(code, message);
                break;
            case "CANCELADA":
                ModelState.AddModelError(code, message);
                break;

            case "ACTIVA":
                ModelState.AddModelError(code, message);
                break;
            default:
                ModelState.AddModelError("", message);
                break;
        }
    }
}