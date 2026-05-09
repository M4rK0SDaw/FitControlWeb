using FitControlWeb.Data;
using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels;
using FitControlWeb.ViewModels.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;

namespace FitControlWeb.Services.Implementations;

public class ClienteDashboardService : IClienteDashboardService
{
    private readonly FitControlDbContext _context;
    private readonly IProfilePhotoService _profilePhotoService;

    public ClienteDashboardService(
        FitControlDbContext context,
        IProfilePhotoService profilePhotoService)
    {
        _context = context;
        _profilePhotoService = profilePhotoService;
    }

    public async Task<ClienteContratarSuscripcionViewModel> GetContratarSuscripcionAsync()
    {
        return new ClienteContratarSuscripcionViewModel
        {
            TiposDisponibles = await _context.TipoSuscripciones
                .Where(t => t.Activo == true)
                .OrderBy(t => t.Precio)
                .ToListAsync()
        };
    }

    public async Task<ServiceResult<string>> CrearCheckoutSuscripcionAsync(
        int usuarioId,
        int tipoSuscripcionId,
        string successUrl,
        string cancelUrl)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == usuarioId && u.Activo == true);

        if (usuario == null)
            return ServiceResult<string>.Fail("El usuario no existe.", "USUARIO_NO_EXISTE");

        var yaActiva = await _context.Suscripciones.AnyAsync(s =>
            s.UsuarioId == usuarioId &&
            s.Activa == true &&
            s.FechaFin >= DateTime.Today);

        if (yaActiva)
            return ServiceResult<string>.Fail("Ya tienes una suscripcion activa.", "SUSCRIPCION_ACTIVA");

        var tipo = await _context.TipoSuscripciones
            .FirstOrDefaultAsync(t => t.Id == tipoSuscripcionId && t.Activo == true);

        if (tipo == null)
            return ServiceResult<string>.Fail("Debes seleccionar un plan valido.", "TIPO_NO_VALIDO");

        var subtotal = tipo.Precio;
        var impuestos = Math.Round(subtotal * 0.21m, 2, MidpointRounding.AwayFromZero);
        var total = subtotal + impuestos;
        var amount = (long)Math.Round(total * 100, MidpointRounding.AwayFromZero);

        if (amount <= 0)
            return ServiceResult<string>.Fail("El importe no es valido.", "IMPORTE_INVALIDO");

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = $"{successUrl}?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = cancelUrl,
            CustomerEmail = usuario.Email,
            ClientReferenceId = $"suscripcion:{usuarioId}:{tipo.Id}",
            Metadata = new Dictionary<string, string>
            {
                { "Flow", "SuscripcionCliente" },
                { "UsuarioId", usuarioId.ToString() },
                { "TipoSuscripcionId", tipo.Id.ToString() },
                { "TipoSuscripcionNombre", tipo.Nombre },
                { "DuracionDias", tipo.DuracionDias.ToString() }
            },
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    { "Flow", "SuscripcionCliente" },
                    { "UsuarioId", usuarioId.ToString() },
                    { "TipoSuscripcionId", tipo.Id.ToString() }
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
                            Name = $"Suscripcion {tipo.Nombre}",
                            Description = $"Duracion: {tipo.DuracionDias} dias. Base: {subtotal:0.00} EUR, IVA: {impuestos:0.00} EUR."
                        }
                    }
                }
            }
        };

        var session = await new SessionService().CreateAsync(options);

        if (string.IsNullOrWhiteSpace(session.Url))
            return ServiceResult<string>.Fail("No se pudo iniciar el pago.", "STRIPE_ERROR");

        return ServiceResult<string>.Ok(session.Url);
    }

    public async Task<ServiceResult<int>> ConfirmarCheckoutSuscripcionAsync(int usuarioId, string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return ServiceResult<int>.Fail("No se ha recibido una sesion de pago valida.", "SESSION_ID_REQUERIDO");

        var session = await new SessionService().GetAsync(sessionId);

        if (session == null || session.PaymentStatus != "paid")
            return ServiceResult<int>.Fail("El pago no aparece como completado.", "STRIPE_NO_PAGADO");

        if (!session.Metadata.TryGetValue("UsuarioId", out var usuarioIdValue) ||
            !int.TryParse(usuarioIdValue, out var usuarioMetadataId) ||
            usuarioMetadataId != usuarioId)
        {
            return ServiceResult<int>.Fail("La sesion no corresponde con este usuario.", "STRIPE_USUARIO_INVALIDO");
        }

        if (!session.Metadata.TryGetValue("TipoSuscripcionId", out var tipoIdValue) ||
            !int.TryParse(tipoIdValue, out var tipoSuscripcionId))
        {
            return ServiceResult<int>.Fail("La sesion no contiene un plan valido.", "TIPO_NO_VALIDO");
        }

        var pagoExistente = await _context.Pagos
            .Include(p => p.Factura)
            .FirstOrDefaultAsync(p =>
                p.ReferenciaExterna == session.Id &&
                p.Activo == true);

        if (pagoExistente != null)
        {
            return ServiceResult<int>.Ok(
                pagoExistente.FacturaId,
                "Pago ya registrado anteriormente.");
        }

        var yaActiva = await _context.Suscripciones.AnyAsync(s =>
            s.UsuarioId == usuarioId &&
            s.Activa == true &&
            s.FechaFin >= DateTime.Today);

        if (yaActiva)
        {
            return ServiceResult<int>.Fail(
                "Ya existe una suscripcion activa para este usuario. No se ha duplicado el cobro en el sistema.",
                "SUSCRIPCION_ACTIVA");
        }

        var tipo = await _context.TipoSuscripciones
            .FirstOrDefaultAsync(t => t.Id == tipoSuscripcionId && t.Activo == true);

        if (tipo == null)
            return ServiceResult<int>.Fail("El plan ya no esta disponible.", "TIPO_NO_VALIDO");

        var fechaInicio = DateTime.Today;
        var fechaFin = fechaInicio.AddDays(tipo.DuracionDias);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var suscripcion = new Suscripcion
        {
            UsuarioId = usuarioId,
            TipoSuscripcionId = tipo.Id,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            Activa = true
        };

        _context.Suscripciones.Add(suscripcion);
        await _context.SaveChangesAsync();

        var tipoFactura = await _context.TipoFacturas
            .FirstOrDefaultAsync(t => t.Nombre == "Suscripcion");

        if (tipoFactura == null)
        {
            tipoFactura = new TipoFactura
            {
                Nombre = "Suscripcion"
            };

            _context.TipoFacturas.Add(tipoFactura);
            await _context.SaveChangesAsync();
        }

        var subtotal = tipo.Precio;
        var impuestos = Math.Round(subtotal * 0.21m, 2, MidpointRounding.AwayFromZero);
        var total = subtotal + impuestos;

        var factura = new Factura
        {
            UsuarioId = usuarioId,
            TipoFacturaId = tipoFactura.Id,
            NumeroFactura = $"FAC-{DateTime.Now:yyyyMMddHHmmss}-SUS-{suscripcion.Id}",
            FechaEmision = DateTime.Now,
            Subtotal = subtotal,
            Impuestos = impuestos,
            Total = total,
            Pagada = true,
            Activo = true
        };

        _context.Facturas.Add(factura);
        await _context.SaveChangesAsync();

        _context.FacturaDetalles.Add(new FacturaDetalle
        {
            FacturaId = factura.Id,
            Concepto = $"Suscripcion {tipo.Nombre} ({fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy})",
            Cantidad = 1,
            PrecioUnitario = subtotal
        });

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

        _context.Pagos.Add(new Pago
        {
            FacturaId = factura.Id,
            MetodoPagoId = metodoStripe.Id,
            Monto = total,
            FechaPago = DateTime.Now,
            ReferenciaExterna = session.Id,
            Activo = true
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return ServiceResult<int>.Ok(
            factura.Id,
            "Suscripcion activada y pago registrado correctamente.");
    }

    public async Task<ClienteDashboardViewModel?> GetDashboardAsync(int usuarioId)
    {
        var hoy = DateTime.Today;
        var hoyDateOnly = DateOnly.FromDateTime(DateTime.Today);

        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario == null)
            return null;

        var suscripcionActual = await _context.Suscripciones
            .Include(s => s.TipoSuscripcion)
            .Where(s =>
                s.UsuarioId == usuarioId &&
                s.Activa == true &&
                s.FechaFin >= hoy)
            .OrderByDescending(s => s.FechaFin)
            .FirstOrDefaultAsync();

        var proximasReservas = await _context.Reservas
            .Include(r => r.Clase)
                .ThenInclude(c => c.Especialidad)
            .Include(r => r.Clase)
                .ThenInclude(c => c.Entrenador)
            .Include(r => r.EstadoReserva)
            .Where(r =>
                r.UsuarioId == usuarioId &&
                r.Activo == true &&
                r.Clase.Fecha >= hoyDateOnly)
            .OrderBy(r => r.Clase.Fecha)
            .ThenBy(r => r.Clase.HoraInicio)
            .Take(5)
            .ToListAsync();

        var facturasPendientes = await _context.Facturas
            .Include(f => f.TipoFactura)
            .Where(f =>
                f.UsuarioId == usuarioId &&
                f.Activo == true &&
                f.Pagada != true)
            .OrderByDescending(f => f.FechaEmision)
            .Take(5)
            .ToListAsync();

        var clasesDisponibles = await _context.Clases
            .Include(c => c.Especialidad)
            .Include(c => c.Entrenador)
            .Include(c => c.Reservas)
            .Where(c =>
                c.Activo == true &&
                c.Fecha >= hoyDateOnly)
            .OrderBy(c => c.Fecha)
            .ThenBy(c => c.HoraInicio)
            .Take(6)
            .ToListAsync();

        var totalReservasActivas = await _context.Reservas
            .CountAsync(r =>
                r.UsuarioId == usuarioId &&
                r.Activo == true);

        var totalFacturasPendientes = await _context.Facturas
            .CountAsync(f =>
                f.UsuarioId == usuarioId &&
                f.Activo == true &&
                f.Pagada != true);

        var importePendiente = await _context.Facturas
            .Where(f =>
                f.UsuarioId == usuarioId &&
                f.Activo == true &&
                f.Pagada != true)
            .SumAsync(f => f.Total);

        return new ClienteDashboardViewModel
        {
            Usuario = usuario,
            SuscripcionActual = suscripcionActual,
            ProximasReservas = proximasReservas,
            FacturasPendientes = facturasPendientes,
            ClasesDisponibles = clasesDisponibles,
            TotalReservasActivas = totalReservasActivas,
            TotalFacturasPendientes = totalFacturasPendientes,
            ImportePendiente = importePendiente
        };
    }

    public async Task<ClientePerfilViewModel?> GetPerfilAsync(int usuarioId)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario == null)
            return null;

        return new ClientePerfilViewModel
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Apellidos = usuario.Apellidos,
            Email = usuario.Email,
            Telefono = usuario.Telefono
        };
    }

    public async Task<ServiceResult> UpdatePerfilAsync(int usuarioId, ClientePerfilViewModel model, IFormFile? foto)
    {
        if (model.Id != usuarioId)
            return ServiceResult.Fail("No puedes modificar el perfil de otro usuario.", "FORBID");

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario == null)
            return ServiceResult.Fail("El usuario no existe.", "NOT_FOUND");

        usuario.Nombre = model.Nombre.Trim();
        usuario.Apellidos = model.Apellidos.Trim();
        usuario.Telefono = model.Telefono;

        if (!string.IsNullOrWhiteSpace(model.NuevaPassword))
        {
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NuevaPassword);
        }

        var fotoResult = await _profilePhotoService.GuardarFotoUsuarioAsync(usuario.Id, foto);

        if (!fotoResult.Success)
            return fotoResult;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Perfil actualizado correctamente.");
    }

    public async Task<(List<Factura> Facturas, int TotalItems, decimal TotalPendiente, int FacturasPendientes, int FacturasPagadas)> GetMisFacturasAsync(
        int usuarioId,
        bool? pagada,
        int page,
        int pageSize)
    {
        var query = _context.Facturas
            .Include(f => f.TipoFactura)
            .Include(f => f.FacturaDetalles)
            .Include(f => f.Pagos)
                .ThenInclude(p => p.MetodoPago)
            .Where(f =>
                f.UsuarioId == usuarioId &&
                f.Activo == true)
            .AsQueryable();

        if (pagada.HasValue)
        {
            query = query.Where(f => f.Pagada == pagada.Value);
        }

        var totalItems = await query.CountAsync();

        var facturas = await query
            .OrderByDescending(f => f.FechaEmision)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPendiente = await _context.Facturas
            .Where(f =>
                f.UsuarioId == usuarioId &&
                f.Activo == true &&
                f.Pagada != true)
            .SumAsync(f => f.Total);

        var facturasPendientes = await _context.Facturas
            .CountAsync(f =>
                f.UsuarioId == usuarioId &&
                f.Activo == true &&
                f.Pagada != true);

        var facturasPagadas = await _context.Facturas
            .CountAsync(f =>
                f.UsuarioId == usuarioId &&
                f.Activo == true &&
                f.Pagada == true);

        return (facturas, totalItems, totalPendiente, facturasPendientes, facturasPagadas);
    }
}
