using FitControlWeb.Helpers;
using FitControlWeb.Data;
using FitControlWeb.ViewModels.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitControlWeb.Controllers;

[Authorize(Roles = "Entrenador")]
public class EntrenadorDashboardController : Controller
{
    private readonly FitControlDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public EntrenadorDashboardController(
        FitControlDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        int entrenadorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var hoy = DateOnly.FromDateTime(DateTime.Today);

        var entrenador = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == entrenadorId);

        if (entrenador == null)
            return NotFound();

        var clasesBase = _context.Clases
            .Include(c => c.Especialidad)
            .Include(c => c.Reservas)
                .ThenInclude(r => r.Usuario)
            .Where(c =>
                c.EntrenadorId == entrenadorId &&
                c.Activo == true);

        var totalClases = await clasesBase.CountAsync();

        var clasesHoy = await clasesBase
            .CountAsync(c => c.Fecha == hoy);

        var proximasClases = await clasesBase
            .CountAsync(c => c.Fecha >= hoy);

        var clasesParaOcupacion = await clasesBase.ToListAsync();

        var plazasTotales = clasesParaOcupacion.Sum(c => c.CapacidadMaxima ?? 0);
        var plazasOcupadas = clasesParaOcupacion.Sum(c => c.Reservas.Count(r => r.Activo == true));

        var ocupacionMedia = plazasTotales > 0
            ? Math.Round((double)plazasOcupadas * 100 / plazasTotales, 2)
            : 0;

        var totalReservas = plazasOcupadas;

        var clasesDeHoy = await _context.Clases
            .Include(c => c.Especialidad)
            .Include(c => c.Reservas)
            .Where(c =>
                c.EntrenadorId == entrenadorId &&
                c.Activo == true &&
                c.Fecha == hoy)
            .OrderBy(c => c.HoraInicio)
            .ToListAsync();

        var proximasClasesListado = await _context.Clases
            .Include(c => c.Especialidad)
            .Include(c => c.Reservas)
            .Where(c =>
                c.EntrenadorId == entrenadorId &&
                c.Activo == true &&
                c.Fecha >= hoy)
            .OrderBy(c => c.Fecha)
            .ThenBy(c => c.HoraInicio)
            .Take(8)
            .ToListAsync();

        var ultimasReservas = await _context.Reservas
            .Include(r => r.Usuario)
            .Include(r => r.Clase)
                .ThenInclude(c => c.Especialidad)
            .Include(r => r.EstadoReserva)
            .Where(r =>
                r.Clase.EntrenadorId == entrenadorId)
            .OrderByDescending(r => r.FechaReserva)
            .Take(8)
            .ToListAsync();

        var vm = new EntrenadorDashboardViewModel
        {
            Entrenador = entrenador,

            TotalClases = totalClases,
            ClasesHoy = clasesHoy,
            ProximasClases = proximasClases,
            TotalReservas = totalReservas,

            PlazasTotales = plazasTotales,
            PlazasOcupadas = plazasOcupadas,
            OcupacionMedia = ocupacionMedia,

            ClasesDeHoy = clasesDeHoy,
            ProximasClasesListado = proximasClasesListado,
            UltimasReservas = ultimasReservas
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Perfil()
    {
        int entrenadorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var entrenador = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == entrenadorId);

        if (entrenador == null)
            return NotFound();

        var vm = new EntrenadorPerfilViewModel
        {
            Id = entrenador.Id,
            Nombre = entrenador.Nombre,
            Apellidos = entrenador.Apellidos,
            Email = entrenador.Email,
            Telefono = entrenador.Telefono
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Perfil(EntrenadorPerfilViewModel model)
    {
        ModelState.Remove(nameof(model.Foto));

        int entrenadorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (model.Id != entrenadorId)
            return Forbid();

        if (!ModelState.IsValid)
            return View(model);

        var entrenador = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == entrenadorId);

        if (entrenador == null)
            return NotFound();

        entrenador.Nombre = model.Nombre.Trim();
        entrenador.Apellidos = model.Apellidos.Trim();
        entrenador.Telefono = model.Telefono;

        if (!string.IsNullOrWhiteSpace(model.NuevaPassword))
        {
            entrenador.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NuevaPassword);
        }

        var foto = Request.Form.Files.FirstOrDefault(f => f.Name == "Foto");

        var fotoResult = await GuardarFotoUsuarioAsync(entrenador.Id, foto);

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

}