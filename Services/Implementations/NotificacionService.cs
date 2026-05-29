using FitControlWeb.Data;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FitControlWeb.Services.Implementations;

public class NotificacionService : INotificacionService
{
    private const string AvisoPrefix = "[Aviso]";
    private const string UrlPrefix = "[Url]";

    private readonly FitControlDbContext _context;
    private readonly ILogger<NotificacionService> _logger;

    public NotificacionService(FitControlDbContext context, ILogger<NotificacionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CrearAsync(int usuarioId, string titulo, string mensaje, string tipo = "info", string? url = null)
    {
        await CrearParaUsuariosAsync(new[] { usuarioId }, titulo, mensaje, tipo, url);
    }

    public async Task CrearParaUsuariosAsync(IEnumerable<int> usuarioIds, string titulo, string mensaje, string tipo = "info", string? url = null)
    {
        var ids = usuarioIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return;

        var remitenteId = await GetRemitenteSistemaAsync(ids);

        if (!remitenteId.HasValue)
        {
            _logger.LogWarning("No se pudo crear notificacion interna porque no hay usuario remitente disponible.");
            return;
        }

        var contenido = ConstruirContenido(titulo, mensaje, tipo, url);
        var ahora = DateTime.Now;

        foreach (var usuarioId in ids.Where(id => id != remitenteId.Value))
        {
            var conversacion = await GetOrCreateConversacionInternaAsync(remitenteId.Value, usuarioId);

            _context.Mensajes.Add(new Mensaje
            {
                ConversacionId = conversacion.Id,
                RemitenteId = remitenteId.Value,
                Contenido = contenido,
                FechaEnvio = ahora,
                Leido = false
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<Notificacion>> GetUltimasAsync(int usuarioId, int take = 50)
    {
        take = take is > 0 and <= 100 ? take : 50;

        var mensajes = await QueryAvisosUsuario(usuarioId)
            .OrderByDescending(m => m.FechaEnvio)
            .Take(take)
            .ToListAsync();

        return mensajes.Select(m => MapMensajeToNotificacion(m, usuarioId)).ToList();
    }

    public async Task<int> CountNoLeidasAsync(int usuarioId)
    {
        return await QueryAvisosUsuario(usuarioId)
            .CountAsync(m => m.Leido != true);
    }

    public async Task MarcarLeidaAsync(int usuarioId, int notificacionId)
    {
        var mensaje = await QueryAvisosUsuario(usuarioId)
            .FirstOrDefaultAsync(m => m.Id == notificacionId);

        if (mensaje == null || mensaje.Leido == true)
            return;

        mensaje.Leido = true;
        await _context.SaveChangesAsync();
    }

    public async Task MarcarTodasLeidasAsync(int usuarioId)
    {
        var mensajes = await QueryAvisosUsuario(usuarioId)
            .Where(m => m.Leido != true)
            .ToListAsync();

        if (mensajes.Count == 0)
            return;

        foreach (var mensaje in mensajes)
        {
            mensaje.Leido = true;
        }

        await _context.SaveChangesAsync();
    }

    private IQueryable<Mensaje> QueryAvisosUsuario(int usuarioId)
    {
        return _context.Mensajes
            .Include(m => m.Conversacion)
            .Where(m =>
                m.RemitenteId != usuarioId &&
                m.Contenido.StartsWith(AvisoPrefix) &&
                (m.Conversacion.Usuario1Id == usuarioId || m.Conversacion.Usuario2Id == usuarioId));
    }

    private async Task<int?> GetRemitenteSistemaAsync(IReadOnlyCollection<int> destinatarios)
    {
        var adminId = await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.Activo == true && u.Rol.Nombre == "Administrador")
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync();

        if (adminId.HasValue)
            return adminId;

        return await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.Activo == true && !destinatarios.Contains(u.Id))
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<Conversacion> GetOrCreateConversacionInternaAsync(int usuario1Id, int usuario2Id)
    {
        var conversacion = await _context.Conversaciones.FirstOrDefaultAsync(c =>
            (c.Usuario1Id == usuario1Id && c.Usuario2Id == usuario2Id) ||
            (c.Usuario1Id == usuario2Id && c.Usuario2Id == usuario1Id));

        if (conversacion != null)
            return conversacion;

        conversacion = new Conversacion
        {
            Usuario1Id = usuario1Id,
            Usuario2Id = usuario2Id,
            FechaCreacion = DateTime.Now
        };

        _context.Conversaciones.Add(conversacion);
        await _context.SaveChangesAsync();

        return conversacion;
    }

    private static string ConstruirContenido(string titulo, string mensaje, string tipo, string? url)
    {
        var lineas = new List<string>
        {
            $"{AvisoPrefix} {tipo}",
            titulo.Trim(),
            mensaje.Trim()
        };

        if (!string.IsNullOrWhiteSpace(url))
            lineas.Add($"{UrlPrefix} {url.Trim()}");

        return string.Join(Environment.NewLine, lineas);
    }

    private static Notificacion MapMensajeToNotificacion(Mensaje mensaje, int usuarioId)
    {
        var lineas = mensaje.Contenido
            .Split(Environment.NewLine, StringSplitOptions.None)
            .ToList();

        var tipo = lineas.Count > 0 && lineas[0].StartsWith(AvisoPrefix)
            ? lineas[0][AvisoPrefix.Length..].Trim()
            : "info";

        var titulo = lineas.Count > 1 ? lineas[1] : "Aviso";
        var mensajeTexto = lineas.Count > 2 ? lineas[2] : mensaje.Contenido;
        var url = lineas.FirstOrDefault(l => l.StartsWith(UrlPrefix))?[UrlPrefix.Length..].Trim();

        return new Notificacion
        {
            Id = mensaje.Id,
            UsuarioId = usuarioId,
            Titulo = titulo,
            Mensaje = mensajeTexto,
            Tipo = string.IsNullOrWhiteSpace(tipo) ? "info" : tipo,
            Url = string.IsNullOrWhiteSpace(url) ? null : url,
            Leida = mensaje.Leido == true,
            FechaCreacion = mensaje.FechaEnvio ?? DateTime.Now
        };
    }
}
