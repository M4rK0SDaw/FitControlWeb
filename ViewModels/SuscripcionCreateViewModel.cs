using FitControlWeb.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace FitControlWeb.ViewModels;

public class SuscripcionCreateViewModel
{
    [Required(ErrorMessage = "Debes seleccionar un cliente.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debes seleccionar un cliente valido.")]
    public int UsuarioId { get; set; }

    [Required(ErrorMessage = "Debes seleccionar un tipo de suscripcion.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debes seleccionar un tipo valido.")]
    public int TipoSuscripcionId { get; set; }

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    public DateTime FechaInicio { get; set; } = DateTime.Today;

    public List<SelectListItem> Usuarios { get; set; } = new();
    public List<SelectListItem> TiposSuscripcion { get; set; } = new();
    public List<TipoSuscripcion> TiposSuscripcionData { get; set; } = new();
}
