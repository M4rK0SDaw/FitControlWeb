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
            .ToListAsync();
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

    public async Task<Mensaje> EnviarMensajeAsync(int conversacionId, int remitenteId, string contenido)
    {
        var mensaje = new Mensaje
        {
            ConversacionId = conversacionId,
            RemitenteId = remitenteId,
            Contenido = contenido,
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
}