using FitControlWeb.Models.Entities;

namespace FitControlWeb.Services.Interfaces;

public interface IAuditoriaService
{
    Task RegistrarAsync(Auditorium auditoria);
    Task<List<Auditorium>> GetAllAsync();
}