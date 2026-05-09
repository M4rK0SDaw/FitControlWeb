using FitControlWeb.ViewModels.Dashboard;

namespace FitControlWeb.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardViewModel> GetAdminDashboardAsync();
}
