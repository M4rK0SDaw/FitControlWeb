
using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;

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

    Task<List<Clase>> GetFiltradasAsync(string search, int? entrenadorId, int? especialidadId, int page, int pageSize);
    Task<int> CountFiltradasAsync(string search, int? entrenadorId, int? especialidadId);
}

//using FitControlWeb.Models.Entities;

//namespace FitControlWeb.Services.Interfaces;

//public interface IClaseService
//{
//    Task<List<Clase>> GetAllAsync();
//    Task<Clase?> GetByIdAsync(int id);

//    Task CreateAsync(Clase clase);
//    Task UpdateAsync(Clase clase);
//    Task SoftDeleteAsync(int id);

//    Task<bool> EntrenadorTieneSolapeAsync(
//     int entrenadorId,
//     DateOnly fecha,
//     TimeOnly horaInicio,
//     TimeOnly horaFin,
//     int? claseIdExcluir = null);

//    Task<List<Clase>> GetFiltradasAsync(
//    string search,
//    int? entrenadorId,
//    int? especialidadId,
//    int page,
//    int pageSize);

//    Task<int> CountFiltradasAsync(
//        string search,
//        int? entrenadorId,
//        int? especialidadId);
//}