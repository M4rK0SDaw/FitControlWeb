
using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;
using FitControlWeb.ViewModels;

namespace FitControlWeb.Services.Interfaces;

public interface IClaseService
{
    Task<List<Clase>> GetAllAsync();
    Task<Clase?> GetByIdAsync(int id);

    Task<ServiceResult> CreateAsync(Clase clase);
    Task<ServiceResult> UpdateAsync(Clase clase);
    Task<ServiceResult> SoftDeleteAsync(int id);

    Task<bool> EntrenadorTieneSolapeAsync(
        int entrenadorId,
        DateOnly fecha,
        TimeOnly horaInicio,
        TimeOnly horaFin,
        int? claseIdExcluir = null);

    Task<List<Clase>> GetFiltradasAsync(string search, int? entrenadorId, int? especialidadId, string? estado, int page, int pageSize);
    Task<int> CountFiltradasAsync(string search, int? entrenadorId, int? especialidadId, string? estado);
    Task<List<ClaseListViewModel>> GetListViewAsync(string search, int? entrenadorId, int? especialidadId, string? estado, int page, int pageSize, int? usuarioClienteId);
    Task<List<Usuario>> GetEntrenadoresActivosAsync();
    Task<List<Especialidad>> GetEspecialidadesActivasAsync();
    Task<bool> PuedeVerClaseAsync(int claseId, int? entrenadorId);
}
