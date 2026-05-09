using FitControlWeb.Data;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FitControlWeb.Services.Implementations;

public class ChatService : IChatService
{
    private readonly FitControlDbContext _context;

    public ChatService(FitControlDbContext context)
    {
        _context = context;
    }

    public async Task<List<Conversacion>> GetConversacionesUsuarioAsync(int usuarioId)
    {
        return await _context.Conversaciones
            .Include(c => c.Usuario1)
            .Include(c => c.Usuario2)
            .Include(c => c.Mensajes)
            .Where(c => c.Usuario1Id == usuarioId || c.Usuario2Id == usuarioId)
            .OrderByDescending(c => c.Mensajes
                .OrderByDescending(m => m.FechaEnvio)
                .Select(m => m.FechaEnvio)
                .FirstOrDefault())
            .ToListAsync();
    }

    public async Task<Conversacion?> GetConversacionAsync(int conversacionId)
    {
        return await _context.Conversaciones
            .Include(c => c.Usuario1)
            .Include(c => c.Usuario2)
            .Include(c => c.Mensajes)
                .ThenInclude(m => m.Remitente)
            .FirstOrDefaultAsync(c => c.Id == conversacionId);
    }

    public async Task<List<Usuario>> GetUsuariosDisponiblesAsync(int usuarioId, bool esCliente)
    {
        var rolDestino = esCliente ? "Entrenador" : "Cliente";

        return await _context.Usuarios
            .Include(u => u.Rol)
            .Where(u =>
                u.Id != usuarioId &&
                u.Activo == true &&
                u.Rol.Nombre == rolDestino)
            .OrderBy(u => u.Nombre)
            .ThenBy(u => u.Apellidos)
            .ToListAsync();
    }

    public async Task<Conversacion?> GetOrCreateConversacionAsync(int usuarioActualId, int otroUsuarioId)
    {
        if (!await PuedeHablarConAsync(usuarioActualId, otroUsuarioId))
            return null;

        var conversacion = await _context.Conversaciones
            .Include(c => c.Usuario1)
            .Include(c => c.Usuario2)
            .Include(c => c.Mensajes)
                .ThenInclude(m => m.Remitente)
            .FirstOrDefaultAsync(c =>
                (c.Usuario1Id == usuarioActualId && c.Usuario2Id == otroUsuarioId) ||
                (c.Usuario1Id == otroUsuarioId && c.Usuario2Id == usuarioActualId));

        if (conversacion != null)
            return conversacion;

        conversacion = await CrearConversacionAsync(usuarioActualId, otroUsuarioId);

        return await _context.Conversaciones
            .Include(c => c.Usuario1)
            .Include(c => c.Usuario2)
            .Include(c => c.Mensajes)
                .ThenInclude(m => m.Remitente)
            .FirstAsync(c => c.Id == conversacion.Id);
    }

    public async Task<Conversacion> CrearConversacionAsync(int usuario1Id, int usuario2Id)
    {
        var conversacion = new Conversacion
        {
            Usuario1Id = usuario1Id,
            Usuario2Id = usuario2Id,
            FechaCreacion = DateTime.Now
        };

        _context.Conversaciones.Add(conversacion);
        await _context.SaveChangesAsync();

        return conversacion;
    }

    public async Task<List<Mensaje>> GetMensajesAsync(int conversacionId)
    {
        return await _context.Mensajes
            .Include(m => m.Remitente)
            .Where(m => m.ConversacionId == conversacionId)
            .OrderBy(m => m.FechaEnvio)
            .ToListAsync();
    }

    public async Task<Mensaje?> EnviarMensajeAsync(int conversacionId, int remitenteId, string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
            return null;

        var conversacion = await _context.Conversaciones
            .FirstOrDefaultAsync(c => c.Id == conversacionId);

        if (conversacion == null)
            return null;

        if (conversacion.Usuario1Id != remitenteId && conversacion.Usuario2Id != remitenteId)
            return null;

        var destinatarioId = conversacion.Usuario1Id == remitenteId
            ? conversacion.Usuario2Id
            : conversacion.Usuario1Id;

        if (!await PuedeHablarConAsync(remitenteId, destinatarioId))
            return null;

        var mensaje = new Mensaje
        {
            ConversacionId = conversacionId,
            RemitenteId = remitenteId,
            Contenido = contenido.Trim(),
            FechaEnvio = DateTime.Now,
            Leido = false
        };

        _context.Mensajes.Add(mensaje);
        await _context.SaveChangesAsync();

        return mensaje;
    }

    public async Task MarcarLeidosAsync(int conversacionId, int usuarioId)
    {
        var mensajes = await _context.Mensajes
            .Where(m =>
                m.ConversacionId == conversacionId &&
                m.RemitenteId != usuarioId &&
                m.Leido == false)
            .ToListAsync();

        foreach (var mensaje in mensajes)
        {
            mensaje.Leido = true;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> PuedeHablarConAsync(int usuarioActualId, int otroUsuarioId)
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

    public async Task<int> CountNoLeidosAsync(int usuarioId)
    {
        return await _context.Mensajes
            .Include(m => m.Conversacion)
            .CountAsync(m =>
                m.RemitenteId != usuarioId &&
                m.Leido != true &&
                (
                    m.Conversacion.Usuario1Id == usuarioId ||
                    m.Conversacion.Usuario2Id == usuarioId
                ));
    }
}
