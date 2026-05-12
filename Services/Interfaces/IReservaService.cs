using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;
using FitControlWeb.ViewModels.Reservas;
using FitControlWeb.ViewModels.Shared;

namespace FitControlWeb.Services.Interfaces;

public interface IReservaService
{
    Task<List<Reserva>> GetByUsuarioAsync(int usuarioId);
    Task<List<Reserva>> GetAllAsync();
    Task<List<Reserva>> GetByClaseAsync(int claseId);
    Task<Reserva?> GetByIdAsync(int id);

    Task<ServiceResult> CrearAsync(int usuarioId, int claseId);
    Task<ServiceResult> CancelarAsync(int reservaId);
    Task<ServiceResult> ReactivarAsync(int reservaId);

    Task<bool> YaReservadaAsync(int usuarioId, int claseId);
    Task<bool> HayPlazasAsync(int claseId);

    Task<List<Reserva>> GetByClasePaginadoAsync(int claseId, int page, int pageSize);
    Task<int> CountByClaseAsync(int claseId);

    Task<List<Reserva>> GetByClaseFiltradoAsync(int claseId, string? search, string? estado, int page, int pageSize);
    Task<int> CountByClaseFiltradoAsync(int claseId, string? search, string? estado);
    Task<List<Reserva>> GetByClaseExportAsync(int claseId, string? search, string? estado);

    Task<List<Reserva>> GetFiltradasAsync(string? search, string? estado, int page, int pageSize);
    Task<int> CountFiltradasAsync(string? search, string? estado);
    Task<List<Reserva>> GetFiltradasAsync(string? search, string? estado, int? entrenadorId, int page, int pageSize);
    Task<int> CountFiltradasAsync(string? search, string? estado, int? entrenadorId);
    Task<int> CountCanceladasAsync(int? entrenadorId);
    Task<Clase?> GetClaseConReservasAsync(int claseId);
    Task<bool> PuedeGestionarClaseAsync(int claseId, int? entrenadorId);
    Task<bool> PuedeGestionarReservaAsync(int reservaId, int? entrenadorId);
    Task<ReservaIndexViewModel> GetIndexViewModelAsync(string? search, string? estado, int page, int pageSize, int? entrenadorId);
    Task<ServiceResult<ReservaPorClaseViewModel>> GetPorClaseViewModelAsync(int claseId, string? search, string? estado, int page, int pageSize, int? entrenadorId);
    Task<ServiceResult<FileContentViewModel>> ExportReservasCsvAsync(int claseId, string? search, string? estado, int? entrenadorId);
    Task<ServiceResult<FileContentViewModel>> ExportReservasExcelAsync(int claseId, string? search, string? estado, int? entrenadorId);
    Task<ServiceResult<FileContentViewModel>> ExportReservasPdfAsync(int claseId, string? search, string? estado, int? entrenadorId);
}
