using FitControlWeb.Data;
using FitControlWeb.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitControlWeb.Controllers;

[Authorize(Roles = "Cliente,Entrenador")]
public class MensajesController : Controller
{
    private readonly FitControlDbContext _context;

    public MensajesController(FitControlDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var conversaciones = await _context.Conversaciones
            .Include(c => c.Usuario1)
            .Include(c => c.Usuario2)
            .Include(c => c.Mensajes)
            .Where(c => c.Usuario1Id == usuarioId || c.Usuario2Id == usuarioId)
            .OrderByDescending(c => c.Mensajes
                .OrderByDescending(m => m.FechaEnvio)
                .Select(m => m.FechaEnvio)
                .FirstOrDefault())
            .ToListAsync();

        ViewBag.UsuarioActualId = usuarioId;

        return View(conversaciones);
    }

    [HttpGet]
    public async Task<IActionResult> Nueva()
    {
        int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (User.IsInRole("Cliente"))
        {
            var entrenadores = await _context.Usuarios
                .Include(u => u.Rol)
                .Where(u =>
                    u.Activo == true &&
                    u.Rol.Nombre == "Entrenador")
                .OrderBy(u => u.Nombre)
                .ToListAsync();

            return View(entrenadores);
        }

        if (User.IsInRole("Entrenador"))
        {
            var clientes = await _context.Usuarios
                .Include(u => u.Rol)
                .Where(u =>
                    u.Activo == true &&
                    u.Rol.Nombre == "Cliente")
                .OrderBy(u => u.Nombre)
                .ToListAsync();

            return View(clientes);
        }

        return Forbid();
    }

    [HttpGet]
    public async Task<IActionResult> Conversacion(int usuarioId)
    {
        int actualId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var otroUsuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == usuarioId && u.Activo == true);

        if (otroUsuario == null)
            return NotFound();

        if (!await PuedeHablarConAsync(actualId, usuarioId))
            return Forbid();

        var conversacion = await _context.Conversaciones
            .Include(c => c.Usuario1)
            .Include(c => c.Usuario2)
            .Include(c => c.Mensajes)
                .ThenInclude(m => m.Remitente)
            .FirstOrDefaultAsync(c =>
                (c.Usuario1Id == actualId && c.Usuario2Id == usuarioId) ||
                (c.Usuario1Id == usuarioId && c.Usuario2Id == actualId));

        if (conversacion == null)
        {
            conversacion = new Conversacion
            {
                Usuario1Id = actualId,
                Usuario2Id = usuarioId,
                FechaCreacion = DateTime.Now
            };

            _context.Conversaciones.Add(conversacion);
            await _context.SaveChangesAsync();

            conversacion = await _context.Conversaciones
                .Include(c => c.Usuario1)
                .Include(c => c.Usuario2)
                .Include(c => c.Mensajes)
                    .ThenInclude(m => m.Remitente)
                .FirstAsync(c => c.Id == conversacion.Id);
        }

        var mensajesNoLeidos = conversacion.Mensajes
            .Where(m => m.RemitenteId != actualId && m.Leido != true)
            .ToList();

        foreach (var mensaje in mensajesNoLeidos)
        {
            mensaje.Leido = true;
        }

        await _context.SaveChangesAsync();

        ViewBag.UsuarioActualId = actualId;
        ViewBag.OtroUsuario = otroUsuario;

        return View(conversacion);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enviar(int conversacionId, string contenido)
    {
        int actualId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (string.IsNullOrWhiteSpace(contenido))
        {
            TempData["Error"] = "El mensaje no puede estar vacío.";
            return RedirectToAction(nameof(Index));
        }

        var conversacion = await _context.Conversaciones
            .FirstOrDefaultAsync(c => c.Id == conversacionId);

        if (conversacion == null)
            return NotFound();

        if (conversacion.Usuario1Id != actualId && conversacion.Usuario2Id != actualId)
            return Forbid();

        int destinatarioId = conversacion.Usuario1Id == actualId
            ? conversacion.Usuario2Id
            : conversacion.Usuario1Id;

        if (!await PuedeHablarConAsync(actualId, destinatarioId))
            return Forbid();

        var mensaje = new Mensaje
        {
            ConversacionId = conversacion.Id,
            RemitenteId = actualId,
            Contenido = contenido.Trim(),
            FechaEnvio = DateTime.Now,
            Leido = false
        };

        _context.Mensajes.Add(mensaje);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Conversacion), new { usuarioId = destinatarioId });
    }

    private async Task<bool> PuedeHablarConAsync(int usuarioActualId, int otroUsuarioId)
    {
        var usuarioActual = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == usuarioActualId);

        var otroUsuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == otroUsuarioId);

        if (usuarioActual == null || otroUsuario == null)
            return false;

        if (usuarioActual.Activo != true || otroUsuario.Activo != true)
            return false;

        if (usuarioActual.Rol.Nombre == "Cliente" && otroUsuario.Rol.Nombre == "Entrenador")
            return true;

        if (usuarioActual.Rol.Nombre == "Entrenador" && otroUsuario.Rol.Nombre == "Cliente")
            return true;

        return false;
    }


    [HttpGet]
    public async Task<IActionResult> ChatPanel()
    {
        int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var conversaciones = await _context.Conversaciones
            .Include(c => c.Usuario1)
            .Include(c => c.Usuario2)
            .Include(c => c.Mensajes)
            .Where(c => c.Usuario1Id == usuarioId || c.Usuario2Id == usuarioId)
            .OrderByDescending(c => c.Mensajes
                .OrderByDescending(m => m.FechaEnvio)
                .Select(m => m.FechaEnvio)
                .FirstOrDefault())
            .ToListAsync();

        ViewBag.UsuarioActualId = usuarioId;

        return PartialView("_ChatPanel", conversaciones);
    }

    [HttpGet]
    public async Task<IActionResult> ChatConversacion(int usuarioId)
    {
        int actualId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var otroUsuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == usuarioId && u.Activo == true);

        if (otroUsuario == null)
            return NotFound();

        if (!await PuedeHablarConAsync(actualId, usuarioId))
            return Forbid();

        var conversacion = await _context.Conversaciones
            .Include(c => c.Usuario1)
            .Include(c => c.Usuario2)
            .Include(c => c.Mensajes)
                .ThenInclude(m => m.Remitente)
            .FirstOrDefaultAsync(c =>
                (c.Usuario1Id == actualId && c.Usuario2Id == usuarioId) ||
                (c.Usuario1Id == usuarioId && c.Usuario2Id == actualId));

        if (conversacion == null)
        {
            conversacion = new Conversacion
            {
                Usuario1Id = actualId,
                Usuario2Id = usuarioId,
                FechaCreacion = DateTime.Now
            };

            _context.Conversaciones.Add(conversacion);
            await _context.SaveChangesAsync();

            conversacion = await _context.Conversaciones
                .Include(c => c.Usuario1)
                .Include(c => c.Usuario2)
                .Include(c => c.Mensajes)
                    .ThenInclude(m => m.Remitente)
                .FirstAsync(c => c.Id == conversacion.Id);
        }

        var noLeidos = conversacion.Mensajes
            .Where(m => m.RemitenteId != actualId && m.Leido != true)
            .ToList();

        foreach (var mensaje in noLeidos)
        {
            mensaje.Leido = true;
        }

        await _context.SaveChangesAsync();

        ViewBag.UsuarioActualId = actualId;
        ViewBag.OtroUsuario = otroUsuario;

        return PartialView("_ChatConversacion", conversacion);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnviarAjax(int conversacionId, string contenido)
    {
        int actualId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (string.IsNullOrWhiteSpace(contenido))
        {
            return BadRequest("El mensaje no puede estar vacío.");
        }

        var conversacion = await _context.Conversaciones
            .FirstOrDefaultAsync(c => c.Id == conversacionId);

        if (conversacion == null)
            return NotFound();

        if (conversacion.Usuario1Id != actualId && conversacion.Usuario2Id != actualId)
            return Forbid();

        int destinatarioId = conversacion.Usuario1Id == actualId
            ? conversacion.Usuario2Id
            : conversacion.Usuario1Id;

        if (!await PuedeHablarConAsync(actualId, destinatarioId))
            return Forbid();

        var mensaje = new Mensaje
        {
            ConversacionId = conversacion.Id,
            RemitenteId = actualId,
            Contenido = contenido.Trim(),
            FechaEnvio = DateTime.Now,
            Leido = false
        };

        _context.Mensajes.Add(mensaje);
        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            usuarioId = destinatarioId
        });
    }

    [HttpGet]
    public async Task<IActionResult> NoLeidos()
    {
        int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var total = await _context.Mensajes
            .Include(m => m.Conversacion)
            .CountAsync(m =>
                m.RemitenteId != usuarioId &&
                m.Leido != true &&
                (
                    m.Conversacion.Usuario1Id == usuarioId ||
                    m.Conversacion.Usuario2Id == usuarioId
                ));

        return Json(new { total });
    }
}