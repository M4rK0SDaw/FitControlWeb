using FitControlWeb.Models.Entities;

namespace FitControlWeb.Services.Interfaces;

public interface IChatService
{
    Task<List<Conversacion>> GetConversacionesUsuarioAsync(int usuarioId);
    Task<Conversacion> CrearConversacionAsync(int usuario1Id, int usuario2Id);

    Task<List<Mensaje>> GetMensajesAsync(int conversacionId);
    Task<Mensaje> EnviarMensajeAsync(int conversacionId, int remitenteId, string contenido);
    Task MarcarLeidosAsync(int conversacionId, int usuarioId);
}