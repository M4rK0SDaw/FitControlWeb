using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;
using FitControlWeb.ViewModels.Facturas;
using FitControlWeb.ViewModels.Shared;

namespace FitControlWeb.Services.Interfaces;

public interface IFacturaService
{
    Task<List<Factura>> GetAllAsync();
    Task<List<Factura>> GetByUsuarioAsync(int usuarioId);
    Task<Factura?> GetByIdAsync(int id);
    Task<bool> PuedeVerFacturaAsync(int facturaId, int usuarioId, bool esAdministrador);

    Task<Factura> CreateAsync(Factura factura);
    Task MarcarComoPagadaAsync(int facturaId);
    Task SoftDeleteAsync(int id);

    Task<ServiceResult<Factura>> CrearDesdeSuscripcionAsync(int suscripcionId);

    Task<List<Factura>> GetFiltradasAsync(string? search, bool? pagada, int page, int pageSize);
    Task<int> CountFiltradasAsync(string? search, bool? pagada);
    Task<FacturaIndexViewModel> GetIndexViewModelAsync(string? search, bool? pagada, int page, int pageSize);
    Task<FileContentViewModel> ExportCsvAsync(string? search, bool? pagada);
    Task<FileContentViewModel> ExportExcelAsync(string? search, bool? pagada);
    Task<ServiceResult<FileContentViewModel>> ExportPdfAsync(string? search, bool? pagada);
    Task<ServiceResult<FileContentViewModel>> GetPdfFileAsync(int facturaId, int usuarioId, bool esAdministrador, bool inline);

    Task<ServiceResult<string>> CrearCheckoutStripeAsync(int facturaId, string successUrl, string cancelUrl);
    Task<ServiceResult> ConfirmarPagoStripeAsync(int facturaId, string sessionId);
}
