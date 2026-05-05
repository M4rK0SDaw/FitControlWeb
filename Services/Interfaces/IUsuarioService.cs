//using FitControlWeb.Models.Entities;

//namespace FitControlWeb.Services.Interfaces;

//public interface IUsuarioService
//{
//    Task<List<Usuario>> GetAllAsync();
//    Task<Usuario?> GetByIdAsync(int id);
//    Task<Usuario?> GetByEmailAsync(string email);

//    Task CreateAsync(Usuario usuario);
//    Task UpdateAsync(Usuario usuario);

//    Task SoftDeleteAsync(int id);
//    Task ActivarAsync(int id);

//    Task<bool> EmailExistsAsync(string email, int? excludeUserId = null); // Task<bool> EmailExistsAsync(string email);

//    Task<List<Usuario>> GetFiltradosAsync(string? search, int? rolId, bool? activo, int page, int pageSize);
//    Task<int> CountFiltradosAsync(string? search, int? rolId, bool? activo);


//}

using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;

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
}