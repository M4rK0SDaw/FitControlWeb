using FitControlWeb.Data;
using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels;
using FitControlWeb.ViewModels.Shared;
using FitControlWeb.ViewModels.Suscripciones;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitControlWeb.Services.Implementations;

public class SuscripcionService : ISuscripcionService
{
    private readonly FitControlDbContext _context;

    public SuscripcionService(FitControlDbContext context)
    {
        _context = context;
    }

    public async Task<List<Suscripcion>> GetFiltradasAsync(string? search, string? estado, int page, int pageSize)
    {
        var suscripciones = await QuerySuscripciones(search)
            .OrderByDescending(s => s.FechaInicio)
            .ToListAsync();

        await MarcarFacturasPendientesAsync(suscripciones);
        return AplicarFiltroEstado(suscripciones, estado)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public async Task<int> CountFiltradasAsync(string? search, string? estado)
    {
        var suscripciones = await QuerySuscripciones(search).ToListAsync();
        await MarcarFacturasPendientesAsync(suscripciones);
        return AplicarFiltroEstado(suscripciones, estado).Count();
    }

    public async Task<Suscripcion?> GetByIdAsync(int id)
    {
        var suscripcion = await _context.Suscripciones
            .Include(s => s.Usuario)
            .Include(s => s.TipoSuscripcion)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (suscripcion != null)
            await MarcarFacturasPendientesAsync(new List<Suscripcion> { suscripcion });

        return suscripcion;
    }

    public async Task<bool> UsuarioTieneSuscripcionActivaAsync(int usuarioId, int? excludeId = null)
    {
        var hoy = DateTime.Today;

        return await _context.Suscripciones.AnyAsync(s =>
            s.UsuarioId == usuarioId &&
            s.Activa == true &&
            s.FechaFin >= hoy &&
            (!excludeId.HasValue || s.Id != excludeId.Value));
    }

    private async Task<bool> UsuarioTieneSuscripcionEnCursoAsync(int usuarioId, int? excludeId = null)
    {
        var hoy = DateTime.Today;

        if (await _context.Suscripciones.AnyAsync(s =>
            s.UsuarioId == usuarioId &&
            (!excludeId.HasValue || s.Id != excludeId.Value) &&
            s.FechaFin >= hoy &&
            s.Activa == true))
        {
            return true;
        }

        var suscripcionesPendientes = await _context.Suscripciones
            .Where(s =>
                s.UsuarioId == usuarioId &&
                (!excludeId.HasValue || s.Id != excludeId.Value) &&
                s.FechaFin >= hoy &&
                s.Activa != true)
            .Select(s => s.Id)
            .ToListAsync();

        foreach (var suscripcionId in suscripcionesPendientes)
        {
            if (await _context.Facturas.AnyAsync(f =>
                f.Activo == true &&
                f.Pagada != true &&
                f.NumeroFactura.EndsWith($"-SUS-{suscripcionId}")))
            {
                return true;
            }
        }

        return false;
    }

    public async Task<ServiceResult> CreateAsync(Suscripcion suscripcion)
    {
        if (suscripcion.FechaFin < suscripcion.FechaInicio)
            return ServiceResult.Fail("La fecha de fin no puede ser anterior a la fecha de inicio.", "FECHAS_INVALIDAS");

        if (await UsuarioTieneSuscripcionEnCursoAsync(suscripcion.UsuarioId))
            return ServiceResult.Fail("El usuario ya tiene una suscripcion activa o pendiente de pago.", "SUSCRIPCION_DUPLICADA");

        suscripcion.Activa = true;

        _context.Suscripciones.Add(suscripcion);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Suscripcion creada correctamente.");
    }

    public async Task<ServiceResult> UpdateAsync(Suscripcion suscripcion)
    {
        if (suscripcion.FechaFin < suscripcion.FechaInicio)
            return ServiceResult.Fail("La fecha de fin no puede ser anterior a la fecha de inicio.", "FECHAS_INVALIDAS");

        if (suscripcion.Activa == true &&
            await UsuarioTieneSuscripcionEnCursoAsync(suscripcion.UsuarioId, suscripcion.Id))
        {
            return ServiceResult.Fail("El usuario ya tiene otra suscripcion activa o pendiente de pago.", "SUSCRIPCION_DUPLICADA");
        }

        _context.Suscripciones.Update(suscripcion);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Suscripcion actualizada correctamente.");
    }

    public async Task<ServiceResult> CancelarAsync(int id)
    {
        var suscripcion = await _context.Suscripciones.FindAsync(id);

        if (suscripcion == null)
            return ServiceResult.Fail("La suscripcion no existe.", "SUSCRIPCION_NO_EXISTE");

        if (suscripcion.FechaFin < DateTime.Today)
            return ServiceResult.Fail("La suscripcion vencida no puede ser cancelada.", "SUSCRIPCION_NO_EXISTE");

        if (suscripcion.Activa != true)
            return ServiceResult.Fail("La suscripcion ya esta cancelada.", "SUSCRIPCION_CANCELADA");

        suscripcion.Activa = false;
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Suscripcion cancelada correctamente.");
    }

    public async Task<ServiceResult> ReactivarAsync(int id)
    {
        var suscripcion = await _context.Suscripciones.FindAsync(id);

        if (suscripcion == null)
            return ServiceResult.Fail("La suscripcion no existe.", "SUSCRIPCION_NO_EXISTE");

        if (suscripcion.Activa == true)
            return ServiceResult.Fail("La suscripcion ya esta activa.", "SUSCRIPCION_ACTIVA");

        if (suscripcion.FechaFin < DateTime.Today)
            return ServiceResult.Fail("No puedes reactivar una suscripcion vencida.", "SUSCRIPCION_VENCIDA");

        if (await UsuarioTieneSuscripcionEnCursoAsync(suscripcion.UsuarioId, suscripcion.Id))
            return ServiceResult.Fail("El usuario ya tiene otra suscripcion activa o pendiente de pago.", "SUSCRIPCION_DUPLICADA");

        suscripcion.Activa = true;
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Suscripcion reactivada correctamente.");
    }

    public async Task<int?> GetFacturaIdAsync(int suscripcionId)
    {
        var factura = await _context.Facturas
            .FirstOrDefaultAsync(f => f.Activo == true && f.NumeroFactura.EndsWith($"-SUS-{suscripcionId}"));

        return factura?.Id;
    }

    public async Task<ServiceResult<SuscripcionCreadaResultViewModel>> CreateFromViewModelAsync(SuscripcionCreateViewModel model)
    {
        var tipo = await _context.TipoSuscripciones
            .FirstOrDefaultAsync(t => t.Id == model.TipoSuscripcionId && t.Activo == true);

        if (tipo == null)
            return ServiceResult<SuscripcionCreadaResultViewModel>.Fail("Debes seleccionar un tipo de suscripcion valido.", "TIPO_NO_VALIDO");

        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == model.UsuarioId && u.Activo == true);

        if (usuario == null || usuario.Rol?.Nombre != "Cliente")
            return ServiceResult<SuscripcionCreadaResultViewModel>.Fail("Debes seleccionar un cliente valido.", "USUARIO_NO_VALIDO");

        if (await UsuarioTieneSuscripcionEnCursoAsync(model.UsuarioId))
            return ServiceResult<SuscripcionCreadaResultViewModel>.Fail("El usuario ya tiene una suscripcion activa o pendiente de pago.", "SUSCRIPCION_DUPLICADA");

        var suscripcion = new Suscripcion
        {
            UsuarioId = model.UsuarioId,
            TipoSuscripcionId = model.TipoSuscripcionId,
            FechaInicio = model.FechaInicio.Date,
            FechaFin = model.FechaInicio.Date.AddDays(tipo.DuracionDias),
            Activa = false
        };

        var tipoFactura = await _context.TipoFacturas
            .FirstOrDefaultAsync(t => t.Nombre == "Suscripcion");

        if (tipoFactura == null)
            return ServiceResult<SuscripcionCreadaResultViewModel>.Fail("No existe el tipo de factura para suscripciones.", "TIPO_FACTURA_NO_VALIDO");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        _context.Suscripciones.Add(suscripcion);
        await _context.SaveChangesAsync();

        var subtotal = tipo.Precio;
        var impuestos = Math.Round(subtotal * 0.21m, 2, MidpointRounding.AwayFromZero);
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

        _context.FacturaDetalles.Add(new FacturaDetalle
        {
            FacturaId = factura.Id,
            Concepto = $"Suscripcion {tipo.Nombre} ({suscripcion.FechaInicio:dd/MM/yyyy} - {suscripcion.FechaFin:dd/MM/yyyy})",
            Cantidad = 1,
            PrecioUnitario = subtotal
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return ServiceResult<SuscripcionCreadaResultViewModel>.Ok(
            new SuscripcionCreadaResultViewModel
            {
                SuscripcionId = suscripcion.Id,
                FacturaId = factura.Id
            },
            "Suscripcion creada y factura generada correctamente.");
    }

    public async Task<ServiceResult> UpdateFromViewModelAsync(SuscripcionEditViewModel model)
    {
        var suscripcion = await GetByIdAsync(model.Id);

        if (suscripcion == null)
            return ServiceResult.Fail("La suscripcion no existe.", "SUSCRIPCION_NO_EXISTE");

        var tieneFactura = await _context.Facturas.AnyAsync(f =>
            f.Activo == true &&
            f.NumeroFactura.EndsWith($"-SUS-{suscripcion.Id}"));

        if (tieneFactura &&
            (
                suscripcion.UsuarioId != model.UsuarioId ||
                suscripcion.TipoSuscripcionId != model.TipoSuscripcionId ||
                suscripcion.FechaInicio.Date != model.FechaInicio.Date
            ))
        {
            return ServiceResult.Fail(
                "Esta suscripcion ya tiene factura generada. No se pueden modificar cliente, plan o fechas para evitar descuadres de facturacion.",
                "SUSCRIPCION_FACTURADA");
        }

        var tipo = await _context.TipoSuscripciones
            .FirstOrDefaultAsync(t => t.Id == model.TipoSuscripcionId && t.Activo == true);

        if (tipo == null)
            return ServiceResult.Fail("Debes seleccionar un tipo de suscripcion valido.", "TIPO_NO_VALIDO");

        suscripcion.UsuarioId = model.UsuarioId;
        suscripcion.TipoSuscripcionId = model.TipoSuscripcionId;
        suscripcion.FechaInicio = model.FechaInicio.Date;
        suscripcion.FechaFin = model.FechaInicio.Date.AddDays(tipo.DuracionDias);
        suscripcion.Activa = model.Activa;

        return await UpdateAsync(suscripcion);
    }

    public async Task<List<Usuario>> GetClientesActivosAsync()
    {
        return await _context.Usuarios
            .Include(u => u.Rol)
            .Where(u => u.Activo == true && u.Rol.Nombre == "Cliente")
            .OrderBy(u => u.Nombre)
            .ThenBy(u => u.Apellidos)
            .ToListAsync();
    }

    public async Task<List<TipoSuscripcion>> GetTiposActivosAsync()
    {
        return await _context.TipoSuscripciones
            .Where(t => t.Activo == true)
            .OrderBy(t => t.Nombre)
            .ToListAsync();
    }

    public async Task<SuscripcionIndexViewModel> GetIndexViewModelAsync(string? search, string? estado, int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 25 or 50 ? pageSize : 10;

        var suscripciones = await GetFiltradasAsync(search, estado, page, pageSize);
        var totalItems = await CountFiltradasAsync(search, estado);

        return new SuscripcionIndexViewModel
        {
            Suscripciones = suscripciones,
            Search = search,
            Estado = estado,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
            TotalSuscripciones = totalItems,
            ActivasPagina = suscripciones.Count(s => s.Activa == true),
            InactivasPagina = suscripciones.Count(s => s.Activa != true && !s.TieneFacturaPendiente),
            VencidasPagina = suscripciones.Count(s => s.Activa == true && s.FechaFin < DateTime.Today)
        };
    }

    public async Task<SuscripcionCreateViewModel> GetCreateViewModelAsync(int? selectedUsuarioId = null)
    {
        var tipos = await GetTiposActivosAsync();

        return new SuscripcionCreateViewModel
        {
            UsuarioId = selectedUsuarioId ?? 0,
            Usuarios = await BuildUsuariosSelectListAsync(selectedUsuarioId),
            TiposSuscripcion = BuildTiposSelectList(tipos),
            TiposSuscripcionData = tipos
        };
    }

    public async Task<SuscripcionEditViewModel?> GetEditViewModelAsync(int id)
    {
        var suscripcion = await GetByIdAsync(id);

        if (suscripcion == null)
            return null;

        var tipos = await GetTiposActivosAsync();

        return new SuscripcionEditViewModel
        {
            Id = suscripcion.Id,
            UsuarioId = suscripcion.UsuarioId,
            TipoSuscripcionId = suscripcion.TipoSuscripcionId,
            FechaInicio = suscripcion.FechaInicio,
            FechaFin = suscripcion.FechaFin,
            Precio = suscripcion.TipoSuscripcion?.Precio ?? 0,
            Activa = suscripcion.Activa ?? true,
            Usuarios = await BuildUsuariosSelectListAsync(suscripcion.UsuarioId),
            TiposSuscripcion = BuildTiposSelectList(tipos, suscripcion.TipoSuscripcionId),
            TiposSuscripcionData = tipos
        };
    }

    public async Task<FileContentViewModel> ExportCsvAsync(string? search, string? estado)
    {
        var suscripciones = await GetFiltradasAsync(search, estado, 1, int.MaxValue);
        var headers = new[] { "Cliente", "Email", "Tipo", "Precio", "Inicio", "Fin", "Estado" };

        return new FileContentViewModel
        {
            Content = ExportHelper.ToCsv(suscripciones, headers, SuscripcionExportRow),
            ContentType = "text/csv",
            FileName = ExportFileNameHelper.Build("suscripciones", "csv")
        };
    }

    public async Task<FileContentViewModel> ExportExcelAsync(string? search, string? estado)
    {
        var suscripciones = await GetFiltradasAsync(search, estado, 1, int.MaxValue);
        var headers = new[] { "Cliente", "Email", "Tipo", "Precio", "Inicio", "Fin", "Estado" };

        return new FileContentViewModel
        {
            Content = ExportHelper.ToExcel(
                suscripciones,
                "Suscripciones",
                "Listado de suscripciones",
                "Suscripciones filtradas",
                GetFiltros(search, estado),
                GetResumen(suscripciones),
                headers,
                s => new object[]
                {
                    $"{s.Usuario?.Nombre ?? ""} {s.Usuario?.Apellidos ?? ""}",
                    s.Usuario?.Email ?? "",
                    s.TipoSuscripcion?.Nombre ?? "",
                    s.TipoSuscripcion?.Precio ?? 0,
                    s.FechaInicio.ToString("dd/MM/yyyy"),
                    s.FechaFin.ToString("dd/MM/yyyy"),
                    EstadoSuscripcion(s)
                }),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileName = ExportFileNameHelper.Build("suscripciones", "xlsx")
        };
    }

    public async Task<FileContentViewModel> ExportPdfAsync(string? search, string? estado)
    {
        var suscripciones = await GetFiltradasAsync(search, estado, 1, int.MaxValue);
        var headers = new[] { "Cliente", "Tipo", "Precio", "Inicio", "Fin", "Estado" };

        return new FileContentViewModel
        {
            Content = ExportHelper.ToPdf(
                suscripciones,
                "Listado de suscripciones",
                "Suscripciones filtradas",
                GetFiltros(search, estado),
                GetResumen(suscripciones),
                headers,
                s => new[]
                {
                    $"{s.Usuario?.Nombre ?? ""} {s.Usuario?.Apellidos ?? ""}",
                    s.TipoSuscripcion?.Nombre ?? "",
                    $"{s.TipoSuscripcion?.Precio.ToString("0.00") ?? "0.00"} EUR",
                    s.FechaInicio.ToString("dd/MM/yyyy"),
                    s.FechaFin.ToString("dd/MM/yyyy"),
                    EstadoSuscripcion(s)
                }),
            ContentType = "application/pdf",
            FileName = ExportFileNameHelper.Build("suscripciones", "pdf")
        };
    }

    private IQueryable<Suscripcion> QuerySuscripciones(string? search)
    {
        var query = _context.Suscripciones
            .Include(s => s.Usuario)
            .Include(s => s.TipoSuscripcion)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s =>
                s.Usuario.Nombre.Contains(term) ||
                s.Usuario.Apellidos.Contains(term) ||
                (s.Usuario.Nombre + " " + s.Usuario.Apellidos).Contains(term) ||
                s.Usuario.Email.Contains(term) ||
                s.TipoSuscripcion.Nombre.Contains(term));
        }

        return query;
    }

    private static IEnumerable<Suscripcion> AplicarFiltroEstado(IEnumerable<Suscripcion> suscripciones, string? estado)
    {
        if (string.IsNullOrWhiteSpace(estado))
            return suscripciones;

        return estado switch
        {
            "Activa" => suscripciones.Where(s => s.Activa == true && s.FechaFin >= DateTime.Today),
            "Pendiente" => suscripciones.Where(s => s.TieneFacturaPendiente),
            "Cancelada" => suscripciones.Where(s => s.Activa != true && !s.TieneFacturaPendiente),
            "Vencida" => suscripciones.Where(s => s.Activa == true && s.FechaFin < DateTime.Today),
            _ => suscripciones
        };
    }

    private async Task MarcarFacturasPendientesAsync(List<Suscripcion> suscripciones)
    {
        foreach (var suscripcion in suscripciones)
        {
            suscripcion.TieneFacturaPendiente = await _context.Facturas.AnyAsync(f =>
                f.Activo == true &&
                f.Pagada != true &&
                f.NumeroFactura.EndsWith($"-SUS-{suscripcion.Id}"));
        }
    }

    private async Task<List<SelectListItem>> BuildUsuariosSelectListAsync(int? selectedId = null)
    {
        return (await GetClientesActivosAsync())
            .Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = $"{u.Nombre} {u.Apellidos} - {u.Email}",
                Selected = selectedId == u.Id
            })
            .OrderBy(u => u.Text)
            .ToList();
    }

    private static List<SelectListItem> BuildTiposSelectList(List<TipoSuscripcion> tipos, int? selectedId = null)
    {
        return tipos
            .Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.Nombre,
                Selected = selectedId == t.Id
            })
            .ToList();
    }

    private static string[] SuscripcionExportRow(Suscripcion suscripcion)
    {
        return new[]
        {
            $"{suscripcion.Usuario?.Nombre ?? ""} {suscripcion.Usuario?.Apellidos ?? ""}",
            suscripcion.Usuario?.Email ?? "",
            suscripcion.TipoSuscripcion?.Nombre ?? "",
            suscripcion.TipoSuscripcion?.Precio.ToString("0.00") ?? "0.00",
            suscripcion.FechaInicio.ToString("dd/MM/yyyy"),
            suscripcion.FechaFin.ToString("dd/MM/yyyy"),
            EstadoSuscripcion(suscripcion)
        };
    }

    private static string EstadoSuscripcion(Suscripcion suscripcion)
    {
        if (suscripcion.TieneFacturaPendiente)
            return "Pendiente de pago";

        if (suscripcion.Activa == true && suscripcion.FechaFin < DateTime.Today)
            return "Vencida";

        return suscripcion.Activa == true ? "Activa" : "Cancelada";
    }

    private static string[] GetFiltros(string? search, string? estado)
    {
        return new[]
        {
            $"Busqueda: {(string.IsNullOrWhiteSpace(search) ? "Sin filtro" : search)}",
            $"Estado: {(string.IsNullOrWhiteSpace(estado) ? "Todos" : estado)}"
        };
    }

    private static List<ReportSummaryItem> GetResumen(List<Suscripcion> suscripciones)
    {
        var hoy = DateTime.Today;

        return new()
        {
            new() { Label = "Total", Value = suscripciones.Count.ToString() },
            new() { Label = "Activas", Value = suscripciones.Count(s => s.Activa == true && s.FechaFin >= hoy).ToString() },
            new() { Label = "Pendientes", Value = suscripciones.Count(s => s.TieneFacturaPendiente).ToString() },
            new() { Label = "Vencidas", Value = suscripciones.Count(s => s.Activa == true && s.FechaFin < hoy).ToString() },
            new() { Label = "Canceladas", Value = suscripciones.Count(s => s.Activa != true && !s.TieneFacturaPendiente).ToString() }
        };
    }
}
