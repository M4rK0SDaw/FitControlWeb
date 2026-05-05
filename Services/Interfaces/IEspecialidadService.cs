using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;

public interface IEspecialidadService
{
    Task<List<Especialidad>> GetAllAsync();
    Task<Especialidad?> GetByIdAsync(int id);

    Task<ServiceResult> CreateAsync(Especialidad especialidad);
    Task<ServiceResult> UpdateAsync(Especialidad especialidad);
    Task<ServiceResult> SoftDeleteAsync(int id);
    Task<ServiceResult> ActivarAsync(int id);

    Task<List<Especialidad>> GetFiltradasAsync(string? search, bool? activo, int page, int pageSize);
    Task<int> CountFiltradasAsync(string? search, bool? activo);

    Task<bool> NombreExisteAsync(string nombre, int? excludeId = null);
}