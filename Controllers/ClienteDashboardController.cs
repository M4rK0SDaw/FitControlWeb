using FitControlWeb.Data;
using FitControlWeb.Helpers;
using FitControlWeb.ViewModels;
using FitControlWeb.ViewModels.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitControlWeb.Controllers;

[Authorize(Roles = "Cliente")]
public class ClienteDashboardController : Controller
{
    private readonly FitControlDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public ClienteDashboardController(
        FitControlDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var hoy = DateTime.Today;
        var hoyDateOnly = DateOnly.FromDateTime(DateTime.Today);

        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        var suscripcionActual = await _context.Suscripciones
            .Include(s => s.TipoSuscripcion)
            .Where(s =>
                s.UsuarioId == usuarioId &&
                s.Activa == true &&
                s.FechaFin >= hoy)
            .OrderByDescending(s => s.FechaFin)
            .FirstOrDefaultAsync();

        var proximasReservas = await _context.Reservas
            .Include(r => r.Clase)
                .ThenInclude(c => c.Especialidad)
            .Include(r => r.Clase)
                .ThenInclude(c => c.Entrenador)
            .Include(r => r.EstadoReserva)
            .Where(r =>
                r.UsuarioId == usuarioId &&
                r.Activo == true &&
                r.Clase.Fecha >= hoyDateOnly)
            .OrderBy(r => r.Clase.Fecha)
            .ThenBy(r => r.Clase.HoraInicio)
            .Take(5)
            .ToListAsync();

        var facturasPendientes = await _context.Facturas
            .Include(f => f.TipoFactura)
            .Where(f =>
                f.UsuarioId == usuarioId &&
                f.Activo == true &&
                f.Pagada != true)
            .OrderByDescending(f => f.FechaEmision)
            .Take(5)
            .ToListAsync();

        var clasesDisponibles = await _context.Clases
            .Include(c => c.Especialidad)
            .Include(c => c.Entrenador)
            .Include(c => c.Reservas)
            .Where(c =>
                c.Activo == true &&
                c.Fecha >= hoyDateOnly)
            .OrderBy(c => c.Fecha)
            .ThenBy(c => c.HoraInicio)
            .Take(6)
            .ToListAsync();

        var totalReservasActivas = await _context.Reservas
            .CountAsync(r =>
                r.UsuarioId == usuarioId &&
                r.Activo == true);

        var totalFacturasPendientes = await _context.Facturas
            .CountAsync(f =>
                f.UsuarioId == usuarioId &&
                f.Activo == true &&
                f.Pagada != true);

        var importePendiente = await _context.Facturas
            .Where(f =>
                f.UsuarioId == usuarioId &&
                f.Activo == true &&
                f.Pagada != true)
            .SumAsync(f => f.Total);

        var vm = new ClienteDashboardViewModel
        {
            Usuario = usuario,
            SuscripcionActual = suscripcionActual,
            ProximasReservas = proximasReservas,
            FacturasPendientes = facturasPendientes,
            ClasesDisponibles = clasesDisponibles,
            TotalReservasActivas = totalReservasActivas,
            TotalFacturasPendientes = totalFacturasPendientes,
            ImportePendiente = importePendiente
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Perfil()
    {
        int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario == null)
            return NotFound();

        var vm = new ClientePerfilViewModel
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Apellidos = usuario.Apellidos,
            Email = usuario.Email,
            Telefono = usuario.Telefono
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Perfil(ClientePerfilViewModel model)
    {
        ModelState.Remove(nameof(model.Foto));

        int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (model.Id != usuarioId)
            return Forbid();

        if (!ModelState.IsValid)
            return View(model);

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario == null)
            return NotFound();

        usuario.Nombre = model.Nombre.Trim();
        usuario.Apellidos = model.Apellidos.Trim();
        usuario.Telefono = model.Telefono;

        if (!string.IsNullOrWhiteSpace(model.NuevaPassword))
        {
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NuevaPassword);
        }

        var foto = Request.Form.Files.FirstOrDefault(f => f.Name == "Foto");

        var fotoResult = await GuardarFotoUsuarioAsync(usuario.Id, foto);

        if (!fotoResult.Success)
        {
            TempData["Error"] = fotoResult.Message;
            return View(model);
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Perfil actualizado correctamente.";
        return RedirectToAction(nameof(Perfil));
    }

    private async Task<ServiceResult> GuardarFotoUsuarioAsync(int usuarioId, IFormFile? foto)
    {
        if (foto == null || foto.Length == 0)
            return ServiceResult.Ok();

        var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(foto.FileName ?? "").ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(extension))
            return ServiceResult.Fail("La imagen no tiene una extensión válida.");

        if (!extensionesPermitidas.Contains(extension))
            return ServiceResult.Fail("Formato de imagen no válido. Usa JPG, PNG o WEBP.");

        if (foto.Length > 5 * 1024 * 1024)
            return ServiceResult.Fail("La imagen no puede superar los 5MB.");

        var carpeta = Path.Combine(_environment.WebRootPath, "uploads", "usuarios");

        Directory.CreateDirectory(carpeta);

        foreach (var archivo in Directory.GetFiles(carpeta, $"usuario-{usuarioId}.*"))
        {
            System.IO.File.Delete(archivo);
        }

        var nombreArchivo = $"usuario-{usuarioId}{extension}";
        var rutaArchivo = Path.Combine(carpeta, nombreArchivo);

        await using var stream = new FileStream(rutaArchivo, FileMode.Create);
        await foto.CopyToAsync(stream);

        return ServiceResult.Ok("Foto guardada correctamente.");
    }


    public async Task<IActionResult> MisFacturas(
    bool? pagada,
    int page = 1,
    int pageSize = 10)
    {
        int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 25 or 50 ? pageSize : 10;

        var query = _context.Facturas
            .Include(f => f.TipoFactura)
            .Include(f => f.Pagos)
                .ThenInclude(p => p.MetodoPago)
            .Where(f =>
                f.UsuarioId == usuarioId &&
                f.Activo == true)
            .AsQueryable();

        if (pagada.HasValue)
        {
            query = query.Where(f => f.Pagada == pagada.Value);
        }

        var totalItems = await query.CountAsync();

        var facturas = await query
            .OrderByDescending(f => f.FechaEmision)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Pagada = pagada;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.TotalFacturas = totalItems;

        ViewBag.TotalPendiente = await _context.Facturas
            .Where(f =>
                f.UsuarioId == usuarioId &&
                f.Activo == true &&
                f.Pagada != true)
            .SumAsync(f => f.Total);

        ViewBag.FacturasPendientes = await _context.Facturas
            .CountAsync(f =>
                f.UsuarioId == usuarioId &&
                f.Activo == true &&
                f.Pagada != true);

        ViewBag.FacturasPagadas = await _context.Facturas
            .CountAsync(f =>
                f.UsuarioId == usuarioId &&
                f.Activo == true &&
                f.Pagada == true);

        return View(facturas);
    }



}