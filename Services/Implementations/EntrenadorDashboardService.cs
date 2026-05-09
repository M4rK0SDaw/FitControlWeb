using FitControlWeb.Data;
using FitControlWeb.Helpers;
using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels;
using FitControlWeb.ViewModels.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace FitControlWeb.Services.Implementations;

public class EntrenadorDashboardService : IEntrenadorDashboardService
{
    private readonly FitControlDbContext _context;
    private readonly IProfilePhotoService _profilePhotoService;

    public EntrenadorDashboardService(
        FitControlDbContext context,
        IProfilePhotoService profilePhotoService)
    {
        _context = context;
        _profilePhotoService = profilePhotoService;
    }

    public async Task<EntrenadorDashboardViewModel?> GetDashboardAsync(int entrenadorId)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);

        var entrenador = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == entrenadorId);

        if (entrenador == null)
            return null;

        var clasesBase = _context.Clases
            .Include(c => c.Especialidad)
            .Include(c => c.Reservas)
                .ThenInclude(r => r.Usuario)
            .Where(c =>
                c.EntrenadorId == entrenadorId &&
                c.Activo == true);

        var totalClases = await clasesBase.CountAsync();
        var clasesHoy = await clasesBase.CountAsync(c => c.Fecha == hoy);
        var proximasClases = await clasesBase.CountAsync(c => c.Fecha >= hoy);

        var clasesParaOcupacion = await clasesBase.ToListAsync();

        var plazasTotales = clasesParaOcupacion.Sum(c => c.CapacidadMaxima ?? 0);
        var plazasOcupadas = clasesParaOcupacion.Sum(c => c.Reservas.Count(r => r.Activo == true));

        var ocupacionMedia = plazasTotales > 0
            ? Math.Round((double)plazasOcupadas * 100 / plazasTotales, 2)
            : 0;

        var clasesDeHoy = await _context.Clases
            .Include(c => c.Especialidad)
            .Include(c => c.Reservas)
            .Where(c =>
                c.EntrenadorId == entrenadorId &&
                c.Activo == true &&
                c.Fecha == hoy)
            .OrderBy(c => c.HoraInicio)
            .ToListAsync();

        var proximasClasesListado = await _context.Clases
            .Include(c => c.Especialidad)
            .Include(c => c.Reservas)
            .Where(c =>
                c.EntrenadorId == entrenadorId &&
                c.Activo == true &&
                c.Fecha >= hoy)
            .OrderBy(c => c.Fecha)
            .ThenBy(c => c.HoraInicio)
            .Take(8)
            .ToListAsync();

        var ultimasReservas = await _context.Reservas
            .Include(r => r.Usuario)
            .Include(r => r.Clase)
                .ThenInclude(c => c.Especialidad)
            .Include(r => r.EstadoReserva)
            .Where(r => r.Clase.EntrenadorId == entrenadorId)
            .OrderByDescending(r => r.FechaReserva)
            .Take(8)
            .ToListAsync();

        return new EntrenadorDashboardViewModel
        {
            Entrenador = entrenador,

            TotalClases = totalClases,
            ClasesHoy = clasesHoy,
            ProximasClases = proximasClases,
            TotalReservas = plazasOcupadas,

            PlazasTotales = plazasTotales,
            PlazasOcupadas = plazasOcupadas,
            OcupacionMedia = ocupacionMedia,

            ClasesDeHoy = clasesDeHoy,
            ProximasClasesListado = proximasClasesListado,
            UltimasReservas = ultimasReservas
        };
    }

    public async Task<EntrenadorPerfilViewModel?> GetPerfilAsync(int entrenadorId)
    {
        var entrenador = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == entrenadorId);

        if (entrenador == null)
            return null;

        return new EntrenadorPerfilViewModel
        {
            Id = entrenador.Id,
            Nombre = entrenador.Nombre,
            Apellidos = entrenador.Apellidos,
            Email = entrenador.Email,
            Telefono = entrenador.Telefono
        };
    }

    public async Task<ServiceResult> UpdatePerfilAsync(int entrenadorId, EntrenadorPerfilViewModel model, IFormFile? foto)
    {
        if (model.Id != entrenadorId)
            return ServiceResult.Fail("No puedes modificar el perfil de otro usuario.", "FORBID");

        var entrenador = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == entrenadorId);

        if (entrenador == null)
            return ServiceResult.Fail("El entrenador no existe.", "NOT_FOUND");

        entrenador.Nombre = model.Nombre.Trim();
        entrenador.Apellidos = model.Apellidos.Trim();
        entrenador.Telefono = model.Telefono;

        if (!string.IsNullOrWhiteSpace(model.NuevaPassword))
        {
            entrenador.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NuevaPassword);
        }

        var fotoResult = await _profilePhotoService.GuardarFotoUsuarioAsync(entrenador.Id, foto);

        if (!fotoResult.Success)
            return fotoResult;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Perfil actualizado correctamente.");
    }
}
