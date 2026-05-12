using FitControlWeb.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace FitControlWeb.ViewModels;

public class SuscripcionEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Debes seleccionar un usuario.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debes seleccionar un usuario valido.")]
    public int UsuarioId { get; set; }

    [Required]
    public int TipoSuscripcionId { get; set; }

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    public DateTime FechaInicio { get; set; }

    [Required(ErrorMessage = "La fecha de fin es obligatoria.")]
    public DateTime FechaFin { get; set; }

    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(0.01, 9999, ErrorMessage = "El precio debe ser mayor que 0.")]
    public decimal Precio { get; set; }

    public bool Activa { get; set; }
    public List<SelectListItem> Usuarios { get; set; } = new();
    public List<SelectListItem> TiposSuscripcion { get; set; } = new();
    public List<TipoSuscripcion> TiposSuscripcionData { get; set; } = new();
}
