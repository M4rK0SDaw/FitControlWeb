using FitControlWeb.Helpers;
using FitControlWeb.ViewModels;
using FitControlWeb.ViewModels.Dashboard;

namespace FitControlWeb.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardViewModel> GetAdminDashboardAsync();
    Task<ClientePerfilViewModel?> GetPerfilAsync(int usuarioId);
    Task<ServiceResult> UpdatePerfilAsync(int usuarioId, ClientePerfilViewModel model, IFormFile? foto);
}
