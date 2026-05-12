using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;
using FitControlWeb.ViewModels;
using FitControlWeb.ViewModels.Shared;
using FitControlWeb.ViewModels.Usuarios;

namespace FitControlWeb.Services.Interfaces;

public interface IUsuarioService
{
    Task<List<Usuario>> GetAllAsync();
    Task<Usuario?> GetByIdAsync(int id);
    Task<Usuario?> GetByEmailAsync(string email);

    Task<ServiceResult> CreateAsync(Usuario usuario);
    Task<ServiceResult> UpdateAsync(Usuario usuario);
    Task<ServiceResult> SoftDeleteAsync(int id);
    Task<ServiceResult> ActivarAsync(int id);

    Task<bool> EmailExistsAsync(string email, int? excludeUserId = null);

    Task<List<Usuario>> GetFiltradosAsync(string? search, int? rolId, bool? activo, int page, int pageSize);
    Task<int> CountFiltradosAsync(string? search, int? rolId, bool? activo);
    Task<List<Rol>> GetRolesAsync();
    Task<(int TotalUsuarios, int UsuariosActivos, int UsuariosInactivos, int TotalClientes, int TotalEntrenadores, int TotalAdministradores, int NuevosEsteMes)> GetKpisAsync();
    Task<ServiceResult<Usuario>> CreateFromViewModelAsync(UsuarioCreateViewModel model, IFormFile? foto);
    Task<ServiceResult> UpdateFromViewModelAsync(UsuarioEditViewModel model);
    Task<ServiceResult> GuardarFotoAsync(int id, IFormFile? foto);

    Task<UsuarioIndexViewModel> GetIndexViewModelAsync(string? search, int? rolId, bool? activo, int page, int pageSize);
    Task<UsuarioCreateViewModel> GetCreateViewModelAsync();
    Task<UsuarioEditViewModel?> GetEditViewModelAsync(int id);
    Task<FileContentViewModel> ExportCsvAsync(string? search, int? rolId, bool? activo);
    Task<FileContentViewModel> ExportExcelAsync(string? search, int? rolId, bool? activo);
    Task<FileContentViewModel> ExportPdfAsync(string? search, int? rolId, bool? activo);
}
