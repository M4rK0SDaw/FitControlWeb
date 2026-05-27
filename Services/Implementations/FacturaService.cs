using FitControlWeb.Data;
using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels.Facturas;
using FitControlWeb.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;

namespace FitControlWeb.Services.Implementations;

public class FacturaService : IFacturaService
{
    private readonly FitControlDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ILogger<FacturaService> _logger;

    public FacturaService(
        FitControlDbContext context,
        IWebHostEnvironment environment,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        ILogger<FacturaService> logger)
    {
        _context = context;
        _environment = environment;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _logger = logger;
    }

    public async Task<List<Factura>> GetAllAsync()
    {
        return await _context.Facturas
            .Include(f => f.Usuario)
            .Include(f => f.TipoFactura)
            .Where(f => f.Activo == true)
            .ToListAsync();
    }

    public async Task<List<Factura>> GetByUsuarioAsync(int usuarioId)
    {
        return await _context.Facturas
            .Include(f => f.FacturaDetalles)
            .Include(f => f.Pagos)
            .Where(f => f.UsuarioId == usuarioId && f.Activo == true)
            .ToListAsync();
    }

    public async Task<Factura?> GetByIdAsync(int id)
    {
        return await _context.Facturas
            .Include(f => f.Usuario)
            .Include(f => f.TipoFactura)
            .Include(f => f.FacturaDetalles)
            .Include(f => f.Pagos)
                .ThenInclude(p => p.MetodoPago)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<bool> PuedeVerFacturaAsync(int facturaId, int usuarioId, bool esAdministrador)
    {
        if (esAdministrador)
            return true;

        return await _context.Facturas.AnyAsync(f =>
            f.Id == facturaId &&
            f.UsuarioId == usuarioId &&
            f.Activo == true);
    }

    public async Task<Factura> CreateAsync(Factura factura)
    {
        factura.FechaEmision = DateTime.Now;
        factura.Activo = true;
        factura.Pagada = false;

        _context.Facturas.Add(factura);
        await _context.SaveChangesAsync();

        return factura;
    }

    public async Task MarcarComoPagadaAsync(int facturaId)
    {
        var factura = await _context.Facturas.FindAsync(facturaId);

        if (factura == null)
            return;

        factura.Pagada = true;
        await _context.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(int id)
    {
        var factura = await _context.Facturas.FindAsync(id);

        if (factura == null)
            return;

        factura.Activo = false;
        factura.FechaBaja = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    public async Task<ServiceResult<Factura>> CrearDesdeSuscripcionAsync(int suscripcionId)
    {
        var facturaExistente = await _context.Facturas
            .FirstOrDefaultAsync(f =>
                f.Activo == true &&
                f.NumeroFactura.EndsWith($"-SUS-{suscripcionId}"));

        if (facturaExistente != null)
        {
            return ServiceResult<Factura>.Ok(
                facturaExistente,
                "Esta suscripcion ya tiene una factura generada.");
        }

        var suscripcion = await _context.Suscripciones
            .Include(s => s.Usuario)
            .Include(s => s.TipoSuscripcion)
            .FirstOrDefaultAsync(s => s.Id == suscripcionId);

        if (suscripcion == null)
            return ServiceResult<Factura>.Fail("La suscripcion no existe.", "SUSCRIPCION_NO_EXISTE");

        if (suscripcion.TipoSuscripcion == null)
            return ServiceResult<Factura>.Fail("La suscripcion no tiene tipo asociado.", "TIPO_NO_EXISTE");

        var tipoFactura = await _context.TipoFacturas
            .FirstOrDefaultAsync(t => t.Nombre == "Suscripcion" || t.Nombre == "Suscripción");

        if (tipoFactura == null)
        {
                tipoFactura = new TipoFactura
                {
                Nombre = "Suscripción"
                };

            _context.TipoFacturas.Add(tipoFactura);
            await _context.SaveChangesAsync();
        }

        var subtotal = suscripcion.TipoSuscripcion.Precio;
        var impuestos = Math.Round(subtotal * 0.21m, 2);
        var total = subtotal + impuestos;

        var factura = new Factura
        {
            UsuarioId = suscripcion.UsuarioId,
            TipoFacturaId = tipoFactura.Id,
            NumeroFactura = $"FAC-{DateTime.Now:yyyyMMddHHmmss}-SUS-{suscripcion.Id}",
            FechaEmision = DateTime.Now,
            Subtotal = subtotal,
            Impuestos = impuestos,
            Total = total,
            Pagada = false,
            Activo = true
        };

        _context.Facturas.Add(factura);
        await _context.SaveChangesAsync();

        var detalle = new FacturaDetalle
        {
            FacturaId = factura.Id,
            Concepto = $"Suscripción {suscripcion.TipoSuscripcion.Nombre} ({suscripcion.FechaInicio:dd/MM/yyyy} - {suscripcion.FechaFin:dd/MM/yyyy})",
            Cantidad = 1,
            PrecioUnitario = subtotal
        };

        _context.FacturaDetalles.Add(detalle);
        await _context.SaveChangesAsync();

        return ServiceResult<Factura>.Ok(factura, "Factura generada correctamente.");
    }

    public async Task<List<Factura>> GetFiltradasAsync(string? search, bool? pagada, int? metodoPagoId, int page, int pageSize)
    {
        return await QueryFacturas(search, pagada, metodoPagoId)
            .OrderByDescending(f => f.FechaEmision)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountFiltradasAsync(string? search, bool? pagada, int? metodoPagoId)
    {
        return await QueryFacturas(search, pagada, metodoPagoId).CountAsync();
    }

    public async Task<FacturaIndexViewModel> GetIndexViewModelAsync(string? search, bool? pagada, int? metodoPagoId, int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 25 or 50 ? pageSize : 10;

        var facturas = await GetFiltradasAsync(search, pagada, metodoPagoId, page, pageSize);
        var totalItems = await CountFiltradasAsync(search, pagada, metodoPagoId);

        return new FacturaIndexViewModel
        {
            Facturas = facturas,
            Search = search,
            Pagada = pagada,
            MetodoPagoId = metodoPagoId,
            MetodosPago = await GetMetodosPagoSelectListAsync(metodoPagoId),
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
            TotalFacturas = totalItems,
            TotalPagadas = facturas.Count(f => f.Pagada == true),
            TotalPendientes = facturas.Count(f => f.Pagada != true),
            ImportePagina = facturas.Sum(f => f.Total)
        };
    }

    public async Task<FileContentViewModel> ExportCsvAsync(string? search, bool? pagada, int? metodoPagoId)
    {
        var facturas = await QueryFacturas(search, pagada, metodoPagoId)
            .OrderByDescending(f => f.FechaEmision)
            .ToListAsync();

        var headers = new[]
        {
            "Numero", "Cliente", "Email", "Tipo", "Metodo pago", "Fecha", "Subtotal", "Impuestos", "Total", "Estado"
        };

        var bytes = ExportHelper.ToCsv(
            facturas,
            headers,
            f => new[]
            {
                f.NumeroFactura,
                $"{f.Usuario?.Nombre ?? ""} {f.Usuario?.Apellidos ?? ""}".Trim(),
                f.Usuario?.Email ?? "",
                f.TipoFactura?.Nombre ?? "",
                GetMetodoPagoTexto(f),
                f.FechaEmision?.ToString("dd/MM/yyyy HH:mm") ?? "",
                f.Subtotal.ToString("0.00"),
                f.Impuestos.ToString("0.00"),
                f.Total.ToString("0.00"),
                f.Pagada == true ? "Pagada" : "Pendiente"
            });

        return new FileContentViewModel
        {
            Content = bytes,
            ContentType = "text/csv",
            FileName = ExportFileNameHelper.Build("facturas", "csv")
        };
    }

    public async Task<FileContentViewModel> ExportExcelAsync(string? search, bool? pagada, int? metodoPagoId)
    {
        var facturas = await QueryFacturas(search, pagada, metodoPagoId)
            .OrderByDescending(f => f.FechaEmision)
            .ToListAsync();

        var bytes = ExportHelper.ToExcel(
            facturas,
            "Facturas",
            "Listado de facturas",
            "Facturas filtradas",
            GetFiltrosExport(search, pagada, metodoPagoId),
            GetResumenExport(facturas),
            new[] { "Numero", "Cliente", "Email", "Tipo", "Metodo pago", "Fecha", "Subtotal", "Impuestos", "Total", "Estado" },
            f => new object[]
            {
                f.NumeroFactura,
                $"{f.Usuario?.Nombre ?? ""} {f.Usuario?.Apellidos ?? ""}".Trim(),
                f.Usuario?.Email ?? "",
                f.TipoFactura?.Nombre ?? "",
                GetMetodoPagoTexto(f),
                f.FechaEmision?.ToString("dd/MM/yyyy HH:mm") ?? "",
                f.Subtotal,
                f.Impuestos,
                f.Total,
                f.Pagada == true ? "Pagada" : "Pendiente"
            });

        return new FileContentViewModel
        {
            Content = bytes,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileName = ExportFileNameHelper.Build("facturas", "xlsx")
        };
    }

    public async Task<ServiceResult<FileContentViewModel>> ExportPdfAsync(string? search, bool? pagada, int? metodoPagoId)
    {
        try
        {
            var facturas = await QueryFacturas(search, pagada, metodoPagoId)
                .OrderByDescending(f => f.FechaEmision)
                .ToListAsync();

            var bytes = ExportHelper.ToPdf(
                facturas,
                "Listado de facturas",
                "Facturas filtradas",
                GetFiltrosExport(search, pagada, metodoPagoId),
                GetResumenExport(facturas),
                new[] { "Numero", "Cliente", "Metodo", "Fecha", "Total", "Estado" },
                f => new[]
                {
                    f.NumeroFactura,
                    $"{f.Usuario?.Nombre ?? ""} {f.Usuario?.Apellidos ?? ""}".Trim(),
                    GetMetodoPagoTexto(f),
                    f.FechaEmision?.ToString("dd/MM/yyyy") ?? "",
                    $"{f.Total:0.00} EUR",
                    f.Pagada == true ? "Pagada" : "Pendiente"
                });

            return ServiceResult<FileContentViewModel>.Ok(new FileContentViewModel
            {
                Content = bytes,
                ContentType = "application/pdf",
                FileName = ExportFileNameHelper.Build("facturas", "pdf")
            });
        }
        catch (Exception ex)
        {
            return ServiceResult<FileContentViewModel>.Fail($"Error al generar PDF: {ex.Message}", "PDF_ERROR");
        }
    }

    public async Task<ServiceResult<FileContentViewModel>> GetPdfFileAsync(int facturaId, int usuarioId, bool esAdministrador, bool inline)
    {
        if (!await PuedeVerFacturaAsync(facturaId, usuarioId, esAdministrador))
            return ServiceResult<FileContentViewModel>.Fail("No tienes permisos para ver esta factura.", "FORBID");

        var factura = await GetByIdAsync(facturaId);

        if (factura == null)
            return ServiceResult<FileContentViewModel>.Fail("La factura no existe.", "NOT_FOUND");

        return ServiceResult<FileContentViewModel>.Ok(new FileContentViewModel
        {
            Content = await GetOrCreateFacturaPdfAsync(factura),
            ContentType = "application/pdf",
            FileName = CrearNombreFacturaPdf(factura.NumeroFactura),
            Inline = inline
        });
    }

    public async Task<ServiceResult<string>> CrearCheckoutStripeAsync(int facturaId, string successUrl, string cancelUrl)
    {
        var factura = await _context.Facturas
            .Include(f => f.Usuario)
            .Include(f => f.FacturaDetalles)
            .FirstOrDefaultAsync(f => f.Id == facturaId && f.Activo == true);

        if (factura == null)
            return ServiceResult<string>.Fail("La factura no existe.", "FACTURA_NO_EXISTE");

        if (factura.Pagada == true)
            return ServiceResult<string>.Fail("La factura ya esta pagada.", "FACTURA_PAGADA");

        var amount = (long)Math.Round(factura.Total * 100, MidpointRounding.AwayFromZero);

        if (amount <= 0)
            return ServiceResult<string>.Fail("El importe de la factura no es valido.", "IMPORTE_INVALIDO");

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = $"{successUrl}?facturaId={factura.Id}&session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{cancelUrl}?facturaId={factura.Id}",
            ClientReferenceId = factura.Id.ToString(),
            CustomerEmail = factura.Usuario?.Email,
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    { "FacturaId", factura.Id.ToString() },
                    { "NumeroFactura", factura.NumeroFactura },
                    { "Subtotal", factura.Subtotal.ToString("0.00") },
                    { "IVA", factura.Impuestos.ToString("0.00") },
                    { "Total", factura.Total.ToString("0.00") }
                }
            },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "eur",
                        UnitAmount = amount,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Factura {factura.NumeroFactura}",
                            Description = $"Subtotal: {factura.Subtotal:0.00} EUR, IVA: {factura.Impuestos:0.00} EUR, Total: {factura.Total:0.00} EUR"
                        }
                    }
                }
            },
            Metadata = new Dictionary<string, string>
            {
                { "FacturaId", factura.Id.ToString() },
                { "NumeroFactura", factura.NumeroFactura },
                { "Origen", "FitControlWeb" }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        if (string.IsNullOrWhiteSpace(session.Url))
            return ServiceResult<string>.Fail("No se pudo crear la sesion de Stripe.", "STRIPE_ERROR");

        return ServiceResult<string>.Ok(session.Url, "Sesion de Stripe creada correctamente.");
    }

    public async Task<ServiceResult> ConfirmarPagoStripeAsync(int facturaId, string sessionId)
    {
        var factura = await _context.Facturas
            .Include(f => f.Usuario)
            .Include(f => f.FacturaDetalles)
            .Include(f => f.Pagos)
            .FirstOrDefaultAsync(f => f.Id == facturaId && f.Activo == true);

        if (factura == null)
            return ServiceResult.Fail("La factura no existe.", "FACTURA_NO_EXISTE");

        if (factura.Pagada == true)
            return ServiceResult.Ok("La factura ya estaba pagada.");

        var sessionService = new SessionService();
        var session = await sessionService.GetAsync(sessionId);

        if (session == null || session.PaymentStatus != "paid")
            return ServiceResult.Fail("El pago todavia no aparece como completado en Stripe.", "STRIPE_NO_PAGADO");

        if (session.ClientReferenceId != factura.Id.ToString())
            return ServiceResult.Fail("La sesion de Stripe no corresponde con esta factura.", "STRIPE_FACTURA_NO_COINCIDE");

        var metodoStripe = await _context.MetodoPagos
            .FirstOrDefaultAsync(m => m.Nombre == "Stripe");

        if (metodoStripe == null)
        {
            metodoStripe = new MetodoPago
            {
                Nombre = "Stripe"
            };

            _context.MetodoPagos.Add(metodoStripe);
            await _context.SaveChangesAsync();
        }

        var pagoExiste = await _context.Pagos.AnyAsync(p =>
            p.FacturaId == factura.Id &&
            p.ReferenciaExterna == session.Id &&
            p.Activo == true);

        if (pagoExiste)
            return ServiceResult.Ok("Pago ya registrado anteriormente.");

        var pago = new Pago
        {
            FacturaId = factura.Id,
            MetodoPagoId = metodoStripe.Id,
            Monto = factura.Total,
            FechaPago = DateTime.Now,
            ReferenciaExterna = session.Id,
            Activo = true
        };

        _context.Pagos.Add(pago);
        factura.Pagada = true;
        await ActivarSuscripcionVinculadaAsync(factura);

        await _context.SaveChangesAsync();
        await EnviarEmailPagoConfirmadoAsync(factura);

        return ServiceResult.Ok("Pago confirmado con Stripe correctamente.");
    }

    private async Task EnviarEmailPagoConfirmadoAsync(Factura factura)
    {
        if (factura.Usuario == null || string.IsNullOrWhiteSpace(factura.Usuario.Email))
            return;

        try
        {
            var template = _emailTemplateService.EmailPagoFactura(factura.Usuario.Nombre, factura);
            var pdf = await GetOrCreateFacturaPdfAsync(factura);

            await _emailService.SendWithAttachmentAsync(
                factura.Usuario.Email,
                template.Subject,
                template.HtmlBody,
                pdf,
                CrearNombreFacturaPdf(factura.NumeroFactura),
                "application/pdf");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo enviar el email de pago confirmado para la factura {FacturaId}", factura.Id);
        }
    }

    private async Task ActivarSuscripcionVinculadaAsync(Factura factura)
    {
        var suscripcionId = ExtraerSuscripcionId(factura.NumeroFactura);

        if (!suscripcionId.HasValue)
            return;

        var suscripcion = await _context.Suscripciones
            .FirstOrDefaultAsync(s => s.Id == suscripcionId.Value);

        if (suscripcion == null)
            return;

        suscripcion.Activa = true;
    }

    private IQueryable<Factura> QueryFacturas(string? search, bool? pagada, int? metodoPagoId)
    {
        var query = _context.Facturas
            .Include(f => f.Usuario)
            .Include(f => f.TipoFactura)
            .Include(f => f.FacturaDetalles)
            .Include(f => f.Pagos)
                .ThenInclude(p => p.MetodoPago)
            .Where(f => f.Activo == true)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(f =>
                f.NumeroFactura.Contains(search) ||
                f.Usuario.Nombre.Contains(search) ||
                f.Usuario.Apellidos.Contains(search) ||
                f.Usuario.Email.Contains(search));
        }

        if (pagada.HasValue)
        {
            query = query.Where(f => f.Pagada == pagada.Value);
        }

        if (metodoPagoId.HasValue)
        {
            query = query.Where(f => f.Pagos.Any(p =>
                p.Activo == true &&
                p.MetodoPagoId == metodoPagoId.Value));
        }

        return query;
    }

    private static string[] GetFiltrosExport(string? search, bool? pagada, int? metodoPagoId)
    {
        return new[]
        {
            $"Busqueda: {(string.IsNullOrWhiteSpace(search) ? "Sin filtro" : search)}",
            $"Pagada: {(pagada.HasValue ? (pagada.Value ? "Si" : "No") : "Todas")}",
            $"MetodoPagoId: {(metodoPagoId.HasValue ? metodoPagoId.Value.ToString() : "Todos")}"
        };
    }

    private static List<ReportSummaryItem> GetResumenExport(List<Factura> facturas)
    {
        return new()
        {
            new() { Label = "Total facturas", Value = facturas.Count.ToString() },
            new() { Label = "Pagadas", Value = facturas.Count(f => f.Pagada == true).ToString() },
            new() { Label = "Pendientes", Value = facturas.Count(f => f.Pagada != true).ToString() },
            new() { Label = "Importe total", Value = facturas.Sum(f => f.Total).ToString("0.00") + " EUR" }
        };
    }

    private static string CrearNombreFacturaPdf(string numeroFactura)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var numeroSeguro = new string(numeroFactura
            .Select(c => invalidChars.Contains(c) ? '-' : c)
            .ToArray());

        return ExportFileNameHelper.BuildFactura(numeroSeguro);
    }

    private async Task<List<SelectListItem>> GetMetodosPagoSelectListAsync(int? selectedId)
    {
        return await _context.MetodoPagos
            .AsNoTracking()
            .Where(m => m.Pagos.Any())
            .OrderBy(m => m.Nombre)
            .Select(m => new SelectListItem
            {
                Value = m.Id.ToString(),
                Text = m.Nombre,
                Selected = selectedId == m.Id
            })
            .ToListAsync();
    }

    private static string GetMetodoPagoTexto(Factura factura)
    {
        return factura.Pagos?
            .Where(p => p.Activo == true)
            .OrderByDescending(p => p.FechaPago)
            .Select(p => p.MetodoPago?.Nombre)
            .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
            ?? (factura.Pagada == true ? "Sin metodo" : "Pendiente");
    }

    private async Task<byte[]> GetOrCreateFacturaPdfAsync(Factura factura)
    {
        var folder = Path.Combine(_environment.ContentRootPath, "App_Data", "FacturasPdf");
        Directory.CreateDirectory(folder);

        var estado = factura.Pagada == true ? "pagada" : "pendiente";
        var filePath = Path.Combine(folder, $"{factura.Id}-{estado}-{CrearNombreFacturaPdf(factura.NumeroFactura)}");

        if (File.Exists(filePath))
            return await File.ReadAllBytesAsync(filePath);

        var logoPath = Path.Combine(_environment.WebRootPath, "img", "logo-fitcontrol-canva-transparent-light.png");
        var bytes = FacturaPdfHelper.GenerarFacturaPdf(factura, logoPath);
        await File.WriteAllBytesAsync(filePath, bytes);

        return bytes;
    }

    private static int? ExtraerSuscripcionId(string numeroFactura)
    {
        const string marker = "-SUS-";
        var index = numeroFactura.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);

        if (index < 0)
            return null;

        var rawId = numeroFactura[(index + marker.Length)..];
        return int.TryParse(rawId, out var suscripcionId) ? suscripcionId : null;
    }
}
