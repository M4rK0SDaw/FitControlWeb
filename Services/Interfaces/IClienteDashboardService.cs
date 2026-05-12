using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;
using FitControlWeb.ViewModels;
using FitControlWeb.ViewModels.Dashboard;

namespace FitControlWeb.Services.Interfaces;

public interface IClienteDashboardService
{
    Task<ClienteDashboardViewModel?> GetDashboardAsync(int usuarioId);
    Task<ClientePerfilViewModel?> GetPerfilAsync(int usuarioId);
    Task<ClienteContratarSuscripcionViewModel> GetContratarSuscripcionAsync(int usuarioId);
    Task<ServiceResult<string>> CrearCheckoutSuscripcionAsync(int usuarioId, int tipoSuscripcionId, string successUrl, string cancelUrl);
    Task<ServiceResult<int>> ConfirmarCheckoutSuscripcionAsync(int usuarioId, string sessionId);
    Task<ServiceResult> UpdatePerfilAsync(int usuarioId, ClientePerfilViewModel model, IFormFile? foto);
    Task<(List<Factura> Facturas, int TotalItems, decimal TotalPendiente, int FacturasPendientes, int FacturasPagadas)> GetMisFacturasAsync(
        int usuarioId,
        bool? pagada,
        int page,
        int pageSize);
}
