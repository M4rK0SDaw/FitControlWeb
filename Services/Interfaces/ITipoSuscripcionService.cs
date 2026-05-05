using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;

namespace FitControlWeb.Services.Interfaces;

public interface ITipoSuscripcionService
{
    Task<List<TipoSuscripcion>> GetFiltradosAsync(string? search, bool? activo, int page, int pageSize);
    Task<int> CountFiltradosAsync(string? search, bool? activo);
    Task<TipoSuscripcion?> GetByIdAsync(int id);

    Task<ServiceResult> CreateAsync(TipoSuscripcion tipo);
    Task<ServiceResult> UpdateAsync(TipoSuscripcion tipo);
    Task<ServiceResult> DesactivarAsync(int id);
    Task<ServiceResult> ActivarAsync(int id);

    Task<bool> NombreExisteAsync(string nombre, int? excludeId = null);
}