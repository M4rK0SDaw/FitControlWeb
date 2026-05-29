using FitControlWeb.Models.Entities;

namespace FitControlWeb.Services.Interfaces;

public interface INotificacionService
{
    Task CrearAsync(int usuarioId, string titulo, string mensaje, string tipo = "info", string? url = null);

    Task CrearParaUsuariosAsync(IEnumerable<int> usuarioIds, string titulo, string mensaje, string tipo = "info", string? url = null);

    Task<List<Notificacion>> GetUltimasAsync(int usuarioId, int take = 50);

    Task<int> CountNoLeidasAsync(int usuarioId);

    Task MarcarLeidaAsync(int usuarioId, int notificacionId);

    Task MarcarTodasLeidasAsync(int usuarioId);
}
