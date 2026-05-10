using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using FitControlWeb.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FitControlWeb.Controllers;

[Authorize(Roles = "Cliente,Entrenador")]
public class MensajesController : Controller
{
    private readonly IChatService _chatService;
    private readonly IHubContext<ChatHub> _hubContext;

    public MensajesController(IChatService chatService, IHubContext<ChatHub> hubContext)
    {
        _chatService = chatService;
        _hubContext = hubContext;
    }

    public async Task<IActionResult> Index()
    {
        var usuarioId = GetUsuarioId();
        var conversaciones = await _chatService.GetConversacionesUsuarioAsync(usuarioId);

        ViewBag.UsuarioActualId = usuarioId;

        return View(conversaciones);
    }

    [HttpGet]
    public async Task<IActionResult> Nueva(string? search)
    {
        var usuarios = await _chatService.GetUsuariosDisponiblesAsync(
            GetUsuarioId(),
            User.IsInRole("Cliente"));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            usuarios = usuarios
                .Where(u =>
                    u.Nombre.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    u.Apellidos.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        ViewBag.Search = search;
        return View(usuarios);
    }

    [HttpGet]
    public async Task<IActionResult> Conversacion(int usuarioId)
    {
        var actualId = GetUsuarioId();

        var conversacion = await _chatService.GetOrCreateConversacionAsync(actualId, usuarioId);

        if (conversacion == null)
            return Forbid();

        await _chatService.MarcarLeidosAsync(conversacion.Id, actualId);

        ViewBag.UsuarioActualId = actualId;
        ViewBag.OtroUsuario = GetOtroUsuario(conversacion, actualId);

        return View(conversacion);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enviar(int conversacionId, string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
        {
            TempData["Error"] = "El mensaje no puede estar vacío.";
            return RedirectToAction(nameof(Index));
        }

        var actualId = GetUsuarioId();
        var conversacion = await _chatService.GetConversacionAsync(conversacionId);

        if (conversacion == null)
            return NotFound();

        if (conversacion.Usuario1Id != actualId && conversacion.Usuario2Id != actualId)
            return Forbid();

        var mensaje = await _chatService.EnviarMensajeAsync(conversacionId, actualId, contenido);

        if (mensaje == null)
            return Forbid();

        var destinatarioId = conversacion.Usuario1Id == actualId
            ? conversacion.Usuario2Id
            : conversacion.Usuario1Id;

        var remitenteNombre = User.FindFirst("NombreCompleto")?.Value ?? "Usuario";
        await NotificarNuevoMensaje(destinatarioId, mensaje, remitenteNombre);

        return RedirectToAction(nameof(Conversacion), new { usuarioId = destinatarioId });
    }

    [HttpGet]
    public async Task<IActionResult> ChatPanel()
    {
        var usuarioId = GetUsuarioId();
        var conversaciones = await _chatService.GetConversacionesUsuarioAsync(usuarioId);

        ViewBag.UsuarioActualId = usuarioId;

        return PartialView("_ChatPanel", conversaciones);
    }

    [HttpGet]
    public async Task<IActionResult> ChatConversacion(int usuarioId)
    {
        var actualId = GetUsuarioId();

        var conversacion = await _chatService.GetOrCreateConversacionAsync(actualId, usuarioId);

        if (conversacion == null)
            return Forbid();

        await _chatService.MarcarLeidosAsync(conversacion.Id, actualId);

        ViewBag.UsuarioActualId = actualId;
        ViewBag.OtroUsuario = GetOtroUsuario(conversacion, actualId);

        return PartialView("_ChatConversacion", conversacion);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnviarAjax(int conversacionId, string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
            return BadRequest("El mensaje no puede estar vacío.");

        var actualId = GetUsuarioId();
        var conversacion = await _chatService.GetConversacionAsync(conversacionId);

        if (conversacion == null)
            return NotFound();

        if (conversacion.Usuario1Id != actualId && conversacion.Usuario2Id != actualId)
            return Forbid();

        var mensaje = await _chatService.EnviarMensajeAsync(conversacionId, actualId, contenido);

        if (mensaje == null)
            return Forbid();

        var destinatarioId = conversacion.Usuario1Id == actualId
            ? conversacion.Usuario2Id
            : conversacion.Usuario1Id;

        var remitenteNombre = User.FindFirst("NombreCompleto")?.Value ?? "Usuario";
        await NotificarNuevoMensaje(destinatarioId, mensaje, remitenteNombre);

        return Json(new
        {
            success = true,
            usuarioId = destinatarioId,
            message = new
            {
                id = mensaje.Id,
                conversacionId = mensaje.ConversacionId,
                contenido = mensaje.Contenido,
                fechaTexto = mensaje.FechaEnvio?.ToString("dd/MM/yyyy HH:mm") ?? "",
                esMio = true,
                remitenteNombre = "Tu",
                leido = mensaje.Leido == true
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarLeidosAjax(int conversacionId)
    {
        var actualId = GetUsuarioId();
        var conversacion = await _chatService.GetConversacionAsync(conversacionId);

        if (conversacion == null)
            return NotFound();

        if (conversacion.Usuario1Id != actualId && conversacion.Usuario2Id != actualId)
            return Forbid();

        await _chatService.MarcarLeidosAsync(conversacionId, actualId);

        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> NoLeidos()
    {
        var total = await _chatService.CountNoLeidosAsync(GetUsuarioId());
        return Json(new { total });
    }

    private int GetUsuarioId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    private static Usuario? GetOtroUsuario(Conversacion conversacion, int usuarioActualId)
    {
        return conversacion.Usuario1Id == usuarioActualId
            ? conversacion.Usuario2
            : conversacion.Usuario1;
    }

    private async Task NotificarNuevoMensaje(int destinatarioId, Mensaje mensaje, string remitenteNombre)
    {
        await _hubContext.Clients.Group($"user:{destinatarioId}")
            .SendAsync("chat:new-message", new
            {
                remitenteId = mensaje.RemitenteId,
                conversacionId = mensaje.ConversacionId,
                contenido = mensaje.Contenido,
                fechaTexto = mensaje.FechaEnvio?.ToString("dd/MM/yyyy HH:mm") ?? "",
                remitenteNombre,
                leido = mensaje.Leido == true
            });
    }
}
