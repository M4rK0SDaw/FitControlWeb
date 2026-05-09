using Stripe.Checkout;
using FitControlWeb.Data;
using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FitControlWeb.Services.Implementations;

public class FacturaService : IFacturaService
{
    private readonly FitControlDbContext _context;

    public FacturaService(FitControlDbContext context)
    {
        _context = context;
    }

    private IQueryable<Factura> QueryFacturas(string? search, bool? pagada)
    {
        var query = _context.Facturas
            .Include(f => f.Usuario)
            .Include(f => f.TipoFactura)
            .Include(f => f.FacturaDetalles)
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

        return query;
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

        if (factura == null) return;

        factura.Pagada = true;

        await _context.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(int id)
    {
        var factura = await _context.Facturas.FindAsync(id);

        if (factura == null) return;

        factura.Activo = false;
        factura.FechaBaja = DateTime.Now;

        await _context.SaveChangesAsync();
    }

    //public async Task<ServiceResult<Factura>> CrearDesdeSuscripcionAsync(int suscripcionId)
    //{
    //    var suscripcion = await _context.Suscripcions
    //        .Include(s => s.Usuario)
    //        .Include(s => s.TipoSuscripcion)
    //        .FirstOrDefaultAsync(s => s.Id == suscripcionId);

    //    if (suscripcion == null)
    //        return ServiceResult<Factura>.Fail("La suscripción no existe.", "SUSCRIPCION_NO_EXISTE");

    //    if (suscripcion.TipoSuscripcion == null)
    //        return ServiceResult<Factura>.Fail("La suscripción no tiene tipo asociado.", "TIPO_NO_EXISTE");

    //    var tipoFactura = await _context.TipoFacturas
    //        .FirstOrDefaultAsync(t => t.Nombre == "Suscripción");

    //    if (tipoFactura == null)
    //    {
    //        tipoFactura = new TipoFactura
    //        {
    //            Nombre = "Suscripción"
    //        };

    //        _context.TipoFacturas.Add(tipoFactura);
    //        await _context.SaveChangesAsync();
    //    }

    //    var subtotal = suscripcion.TipoSuscripcion.Precio;
    //    var impuestos = Math.Round(subtotal * 0.21m, 2);
    //    var total = subtotal + impuestos;

    //    var factura = new Factura
    //    {
    //        UsuarioId = suscripcion.UsuarioId,
    //        TipoFacturaId = tipoFactura.Id,
    //        NumeroFactura = $"FAC-{DateTime.Now:yyyyMMddHHmmss}-{suscripcion.Id}",
    //        FechaEmision = DateTime.Now,
    //        Subtotal = subtotal,
    //        Impuestos = impuestos,
    //        Total = total,
    //        Pagada = false,
    //        Activo = true
    //    };

    //    _context.Facturas.Add(factura);
    //    await _context.SaveChangesAsync();

    //    var detalle = new FacturaDetalle
    //    {
    //        FacturaId = factura.Id,
    //        Concepto = $"Suscripción {suscripcion.TipoSuscripcion.Nombre} ({suscripcion.FechaInicio:dd/MM/yyyy} - {suscripcion.FechaFin:dd/MM/yyyy})",
    //        Cantidad = 1,
    //        PrecioUnitario = subtotal
    //    };

    //    _context.FacturaDetalles.Add(detalle);
    //    await _context.SaveChangesAsync();

    //    return ServiceResult<Factura>.Ok(factura, "Factura generada correctamente.");
    //}

    
    public async Task<List<Factura>> GetFiltradasAsync(
        string? search,
        bool? pagada,
        int page,
        int pageSize)
    {
        return await QueryFacturas(search, pagada)
            .OrderByDescending(f => f.FechaEmision)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountFiltradasAsync(string? search, bool? pagada)
    {
        return await QueryFacturas(search, pagada).CountAsync();
    }
    public async Task<ServiceResult<string>> CrearCheckoutStripeAsync(
    int facturaId,
    string successUrl,
    string cancelUrl)
    {
        var factura = await _context.Facturas
            .Include(f => f.Usuario)
            .Include(f => f.FacturaDetalles)
            .FirstOrDefaultAsync(f => f.Id == facturaId && f.Activo == true);

        if (factura == null)
            return ServiceResult<string>.Fail("La factura no existe.", "FACTURA_NO_EXISTE");

        if (factura.Pagada == true)
            return ServiceResult<string>.Fail("La factura ya está pagada.", "FACTURA_PAGADA");

        var amount = (long)Math.Round(factura.Total * 100, MidpointRounding.AwayFromZero);

        if (amount <= 0)
            return ServiceResult<string>.Fail("El importe de la factura no es válido.", "IMPORTE_INVALIDO");

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
                        Description = $"Subtotal: {factura.Subtotal:0.00} €, IVA: {factura.Impuestos:0.00} €, Total: {factura.Total:0.00} €"
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
            return ServiceResult<string>.Fail("No se pudo crear la sesión de Stripe.", "STRIPE_ERROR");

        return ServiceResult<string>.Ok(session.Url, "Sesión de Stripe creada correctamente.");
    }

    //public async Task<ServiceResult<string>> CrearCheckoutStripeAsync(
    //int facturaId,
    //string successUrl,
    //string cancelUrl)
    //{
    //    var factura = await _context.Facturas
    //        .Include(f => f.Usuario)
    //        .FirstOrDefaultAsync(f => f.Id == facturaId && f.Activo == true);

    //    if (factura == null)
    //        return ServiceResult<string>.Fail("La factura no existe.", "FACTURA_NO_EXISTE");

    //    if (factura.Pagada == true)
    //        return ServiceResult<string>.Fail("La factura ya está pagada.", "FACTURA_PAGADA");

    //    var amount = (long)Math.Round(factura.Total * 100);

    //    if (amount <= 0)
    //        return ServiceResult<string>.Fail("El importe de la factura no es válido.", "IMPORTE_INVALIDO");

    //    var options = new SessionCreateOptions
    //    {
    //        Mode = "payment",
    //        SuccessUrl = successUrl + "?facturaId=" + factura.Id + "&session_id={CHECKOUT_SESSION_ID}",
    //        CancelUrl = cancelUrl + "?facturaId=" + factura.Id,
    //        ClientReferenceId = factura.Id.ToString(),
    //        CustomerEmail = factura.Usuario?.Email,
    //        LineItems = new List<SessionLineItemOptions>
    //    {
    //        new()
    //        {
    //            Quantity = 1,
    //            PriceData = new SessionLineItemPriceDataOptions
    //            {
    //                Currency = "eur",
    //                UnitAmount = amount,
    //                ProductData = new SessionLineItemPriceDataProductDataOptions
    //                {
    //                    Name = $"Factura {factura.NumeroFactura}"
    //                }
    //            }
    //        }
    //    },
    //        Metadata = new Dictionary<string, string>
    //    {
    //        { "FacturaId", factura.Id.ToString() },
    //        { "NumeroFactura", factura.NumeroFactura },
    //        { "Subtotal", factura.Subtotal.ToString("0.00") },
    //        { "IVA", factura.Impuestos.ToString("0.00") },
    //        { "Total", factura.Total.ToString("0.00") },
    //        { "Tipo", "Factura FitControl" }
    //    }
    //    };

    //    var service = new SessionService();
    //    var session = await service.CreateAsync(options);

    //    if (string.IsNullOrWhiteSpace(session.Url))
    //        return ServiceResult<string>.Fail("No se pudo crear la sesión de Stripe.", "STRIPE_ERROR");

    //    return ServiceResult<string>.Ok(session.Url, "Sesión de Stripe creada correctamente.");
    //}


    public async Task<ServiceResult> ConfirmarPagoStripeAsync(int facturaId, string sessionId)
    {



        var factura = await _context.Facturas
            .Include(f => f.Pagos)
            .FirstOrDefaultAsync(f => f.Id == facturaId && f.Activo == true);

        if (factura == null)
            return ServiceResult.Fail("La factura no existe.", "FACTURA_NO_EXISTE");

        if (factura.Pagada == true)
            return ServiceResult.Ok("La factura ya estaba pagada.");

        var sessionService = new SessionService();
        var session = await sessionService.GetAsync(sessionId);

        if (session == null || session.PaymentStatus != "paid")
            return ServiceResult.Fail("El pago todavía no aparece como completado en Stripe.", "STRIPE_NO_PAGADO");

        if (session.ClientReferenceId != factura.Id.ToString())
            return ServiceResult.Fail("La sesión de Stripe no corresponde con esta factura.", "STRIPE_FACTURA_NO_COINCIDE");

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

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Pago confirmado con Stripe correctamente.");
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
                "Esta suscripción ya tiene una factura generada.");
        }

        var suscripcion = await _context.Suscripciones
            .Include(s => s.Usuario)
            .Include(s => s.TipoSuscripcion)
            .FirstOrDefaultAsync(s => s.Id == suscripcionId);

        if (suscripcion == null)
            return ServiceResult<Factura>.Fail("La suscripción no existe.", "SUSCRIPCION_NO_EXISTE");

        if (suscripcion.TipoSuscripcion == null)
            return ServiceResult<Factura>.Fail("La suscripción no tiene tipo asociado.", "TIPO_NO_EXISTE");

        var tipoFactura = await _context.TipoFacturas
            .FirstOrDefaultAsync(t => t.Nombre == "Suscripción");

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


}
