using System.Security.Claims;
using FitControlWeb.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitControlWeb.Controllers;

[Authorize]
public class NotificacionesController : Controller
{
    private readonly INotificacionService _notificacionService;

    public NotificacionesController(INotificacionService notificacionService)
    {
        _notificacionService = notificacionService;
    }

    [HttpGet]
    public async Task<IActionResult> Mis()
    {
        var usuarioId = GetUsuarioId();
        if (!usuarioId.HasValue)
            return Unauthorized();

        var notificaciones = await _notificacionService.GetUltimasAsync(usuarioId.Value);
        var totalNoLeidas = await _notificacionService.CountNoLeidasAsync(usuarioId.Value);

        return Json(new
        {
            totalNoLeidas,
            items = notificaciones.Select(n => new
            {
                n.Id,
                n.Titulo,
                n.Mensaje,
                n.Tipo,
                n.Url,
                n.Leida,
                Fecha = n.FechaCreacion.ToString("dd/MM/yyyy HH:mm")
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> NoLeidas()
    {
        var usuarioId = GetUsuarioId();
        if (!usuarioId.HasValue)
            return Unauthorized();

        return Json(new { total = await _notificacionService.CountNoLeidasAsync(usuarioId.Value) });
    }

    [HttpPost]
    public async Task<IActionResult> MarcarLeida(int id)
    {
        var usuarioId = GetUsuarioId();
        if (!usuarioId.HasValue)
            return Unauthorized();

        await _notificacionService.MarcarLeidaAsync(usuarioId.Value, id);
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> MarcarTodasLeidas()
    {
        var usuarioId = GetUsuarioId();
        if (!usuarioId.HasValue)
            return Unauthorized();

        await _notificacionService.MarcarTodasLeidasAsync(usuarioId.Value);
        return Json(new { success = true });
    }

    private int? GetUsuarioId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var usuarioId) ? usuarioId : null;
    }
}
