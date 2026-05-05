using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;

namespace FitControlWeb.Services.Interfaces;

public interface IMetodoPagoService
{
    Task<List<MetodoPago>> GetFiltradosAsync(string? search, int page, int pageSize);
    Task<int> CountFiltradosAsync(string? search);

    Task<MetodoPago?> GetByIdAsync(int id);

    Task<ServiceResult> CreateAsync(MetodoPago metodoPago);
    Task<ServiceResult> UpdateAsync(MetodoPago metodoPago);
    Task<ServiceResult> DeleteAsync(int id);

    Task<bool> NombreExisteAsync(string nombre, int? excludeId = null);
}