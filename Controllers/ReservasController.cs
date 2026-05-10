using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;
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
    public async Task<IActionResult> Index(
        string? search,
        string? estado,
        int page = 1,
        int pageSize = 10)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 25 or 50 ? pageSize : 10;

        var entrenadorId = User.IsInRole("Entrenador") ? GetUsuarioId() : (int?)null;
        var reservas = await _reservaService.GetFiltradasAsync(search, estado, entrenadorId, page, pageSize);
        var totalItems = await _reservaService.CountFiltradasAsync(search, estado, entrenadorId);

        ViewBag.Search = search;
        ViewBag.Estado = estado;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.TotalReservas = totalItems;
        ViewBag.PlazasCanceladas = await _reservaService.CountCanceladasAsync(entrenadorId);

        return View(reservas);
    }

    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> MisReservas()
    {
        var reservas = await _reservaService.GetByUsuarioAsync(GetUsuarioId());
        return View(reservas);
    }

    [Authorize(Roles = "Cliente")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reservar(int claseId, string? returnUrl)
    {
        var result = await _reservaService.CrearAsync(GetUsuarioId(), claseId);

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
    public async Task<IActionResult> PorClase(
        int claseId,
        string? search,
        string? estado,
        int page = 1,
        int pageSize = 10)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 25 or 50 ? pageSize : 10;

        var entrenadorId = User.IsInRole("Entrenador") ? GetUsuarioId() : (int?)null;
        var clase = await _reservaService.GetClaseConReservasAsync(claseId);

        if (clase == null)
            return NotFound();

        if (!await _reservaService.PuedeGestionarClaseAsync(claseId, entrenadorId))
            return Forbid();

        var reservas = await _reservaService.GetByClaseFiltradoAsync(claseId, search, estado, page, pageSize);
        var totalItems = await _reservaService.CountByClaseFiltradoAsync(claseId, search, estado);

        CargarViewBagClase(clase, search, estado, page, pageSize, totalItems);

        return View(reservas);
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportReservasCsv(int claseId, string? search, string? estado)
    {
        var forbid = await ValidarAccesoClaseAsync(claseId);
        if (forbid != null) return forbid;

        var reservas = await _reservaService.GetByClaseExportAsync(claseId, search, estado);
        var headers = new[] { "Cliente", "Email", "Clase", "Fecha reserva", "Estado" };

        var bytes = ExportHelper.ToCsv(reservas, headers, ReservaExportRow);

        return File(bytes, "text/csv", "reservas-clase.csv");
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportReservasExcel(int claseId, string? search, string? estado)
    {
        var forbid = await ValidarAccesoClaseAsync(claseId);
        if (forbid != null) return forbid;

        var reservas = await _reservaService.GetByClaseExportAsync(claseId, search, estado);
        var primera = reservas.FirstOrDefault();

        var subtitle = primera?.Clase != null
            ? $"{primera.Clase.Nombre} - {primera.Clase.Fecha:dd/MM/yyyy} - {primera.Clase.HoraInicio} a {primera.Clase.HoraFin}"
            : "Reservas de la clase";

        var bytes = ExportHelper.ToExcel(
            reservas,
            "Reservas",
            "Reservas por clase",
            subtitle,
            GetFiltros(search, estado),
            GetResumen(reservas),
            new[] { "Cliente", "Email", "Clase", "Fecha reserva", "Estado" },
            r => ReservaExportRow(r).Cast<object>().ToArray());

        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "reservas-clase.xlsx");
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ExportReservasPdf(int claseId, string? search, string? estado)
    {
        try
        {
            var forbid = await ValidarAccesoClaseAsync(claseId);
            if (forbid != null) return forbid;

            var reservas = await _reservaService.GetByClaseExportAsync(claseId, search, estado);

            var bytes = ExportHelper.ToPdf(
                reservas,
                "Reservas por clase",
                "Listado de reservas filtradas",
                GetFiltros(search, estado),
                GetResumen(reservas),
                new[] { "Cliente", "Email", "Clase", "Fecha reserva", "Estado" },
                ReservaExportRow);

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

    private async Task<IActionResult?> ValidarAccesoClaseAsync(int claseId)
    {
        var entrenadorId = User.IsInRole("Entrenador") ? GetUsuarioId() : (int?)null;

        if (!await _reservaService.PuedeGestionarClaseAsync(claseId, entrenadorId))
            return Forbid();

        return null;
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

    private void CargarViewBagClase(Clase clase, string? search, string? estado, int page, int pageSize, int totalItems)
    {
        ViewBag.ClaseId = clase.Id;
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
    }

    private static string[] ReservaExportRow(Reserva r)
    {
        return new[]
        {
            $"{r.Usuario?.Nombre ?? ""} {r.Usuario?.Apellidos ?? ""}",
            r.Usuario?.Email ?? "",
            r.Clase?.Nombre ?? "",
            r.FechaReserva?.ToString("dd/MM/yyyy HH:mm") ?? "",
            r.EstadoReserva?.Nombre ?? ""
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

    private static List<ReportSummaryItem> GetResumen(List<Reserva> reservas)
    {
        return new()
        {
            new() { Label = "Total", Value = reservas.Count.ToString() },
            new() { Label = "Activas", Value = reservas.Count(r => r.Activo == true).ToString() },
            new() { Label = "Canceladas", Value = reservas.Count(r => r.Activo != true).ToString() }
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

    private IActionResult RedirectLocalOr(string? returnUrl, string action, string controller)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(action, controller);
    }
}
