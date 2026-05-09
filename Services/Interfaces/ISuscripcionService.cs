using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;
using FitControlWeb.ViewModels;

namespace FitControlWeb.Services.Interfaces;

public interface ISuscripcionService
{
    Task<List<Suscripcion>> GetFiltradasAsync(string? search, string? estado, int page, int pageSize);
    Task<int> CountFiltradasAsync(string? search, string? estado);
    Task<Suscripcion?> GetByIdAsync(int id);

    Task<ServiceResult> CreateAsync(Suscripcion suscripcion);
    Task<ServiceResult> UpdateAsync(Suscripcion suscripcion);
    Task<ServiceResult> CancelarAsync(int id);
    Task<ServiceResult> ReactivarAsync(int id);

    Task<bool> UsuarioTieneSuscripcionActivaAsync(int usuarioId, int? excludeId = null);
    Task<int?> GetFacturaIdAsync(int suscripcionId);
    Task<ServiceResult> CreateFromViewModelAsync(SuscripcionCreateViewModel model);
    Task<ServiceResult> UpdateFromViewModelAsync(SuscripcionEditViewModel model);
    Task<List<Usuario>> GetClientesActivosAsync();
    Task<List<TipoSuscripcion>> GetTiposActivosAsync();
}
