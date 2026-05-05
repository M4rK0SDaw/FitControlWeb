using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;

namespace FitControlWeb.Services.Interfaces;

public interface IFacturaService
{
    Task<List<Factura>> GetAllAsync();
    Task<List<Factura>> GetByUsuarioAsync(int usuarioId);
    Task<Factura?> GetByIdAsync(int id);

    Task<Factura> CreateAsync(Factura factura);
    Task MarcarComoPagadaAsync(int facturaId);
    Task SoftDeleteAsync(int id);

    Task<ServiceResult<Factura>> CrearDesdeSuscripcionAsync(int suscripcionId);

    Task<ServiceResult> SimularPagoStripeAsync(int facturaId);

    Task<List<Factura>> GetFiltradasAsync(string? search, bool? pagada, int page, int pageSize);
    Task<int> CountFiltradasAsync(string? search, bool? pagada);

    Task<ServiceResult<string>> CrearCheckoutStripeAsync(int facturaId, string successUrl, string cancelUrl);
    Task<ServiceResult> ConfirmarPagoStripeAsync(int facturaId, string sessionId);
}