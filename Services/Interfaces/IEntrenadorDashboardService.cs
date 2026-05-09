using FitControlWeb.Helpers;
using FitControlWeb.ViewModels;
using FitControlWeb.ViewModels.Dashboard;

namespace FitControlWeb.Services.Interfaces;

public interface IEntrenadorDashboardService
{
    Task<EntrenadorDashboardViewModel?> GetDashboardAsync(int entrenadorId);
    Task<EntrenadorPerfilViewModel?> GetPerfilAsync(int entrenadorId);
    Task<ServiceResult> UpdatePerfilAsync(int entrenadorId, EntrenadorPerfilViewModel model, IFormFile? foto);
}
