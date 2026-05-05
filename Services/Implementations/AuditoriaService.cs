using FitControlWeb.Data;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FitControlWeb.Services.Implementations;

public class AuditoriaService : IAuditoriaService
{
    private readonly FitControlDbContext _context;

    public AuditoriaService(FitControlDbContext context)
    {
        _context = context;
    }

    public async Task RegistrarAsync(Auditorium auditoria)
    {
        auditoria.Fecha = DateTime.Now;

        _context.Auditoria.Add(auditoria);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Auditorium>> GetAllAsync()
    {
        return await _context.Auditoria
            .OrderByDescending(a => a.Fecha)
            .ToListAsync();
    }
}