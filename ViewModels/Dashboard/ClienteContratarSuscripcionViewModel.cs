using System.ComponentModel.DataAnnotations;
using FitControlWeb.Models.Entities;

namespace FitControlWeb.ViewModels.Dashboard;

public class ClienteContratarSuscripcionViewModel
{
    [Required(ErrorMessage = "Debes seleccionar un plan.")]
    [Range(1, int.MaxValue, ErrorMessage = "Plan no valido.")]
    public int TipoSuscripcionId { get; set; }

    public List<TipoSuscripcion> TiposDisponibles { get; set; } = new();
    public bool TieneSuscripcionActiva { get; set; }
    public string? NombreSuscripcionActiva { get; set; }
    public DateTime? FechaFinSuscripcionActiva { get; set; }
}
