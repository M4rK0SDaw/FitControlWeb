using FitControlWeb.Models.Entities;

namespace FitControlWeb.Services.Interfaces;

public interface IChatService
{
    Task<List<Conversacion>> GetConversacionesUsuarioAsync(int usuarioId);
    Task<Conversacion?> GetConversacionAsync(int conversacionId);
    Task<List<Usuario>> GetUsuariosDisponiblesAsync(int usuarioId, bool esCliente);
    Task<Conversacion?> GetOrCreateConversacionAsync(int usuarioActualId, int otroUsuarioId);
    Task<Conversacion> CrearConversacionAsync(int usuario1Id, int usuario2Id);

    Task<List<Mensaje>> GetMensajesAsync(int conversacionId);
    Task<Mensaje?> EnviarMensajeAsync(int conversacionId, int remitenteId, string contenido);
    Task MarcarLeidosAsync(int conversacionId, int usuarioId);
    Task<bool> PuedeHablarConAsync(int usuarioActualId, int otroUsuarioId);
    Task<int> CountNoLeidosAsync(int usuarioId);
}
