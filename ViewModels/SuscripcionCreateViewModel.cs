//using System.ComponentModel.DataAnnotations;

//namespace FitControlWeb.ViewModels;

//public class SuscripcionCreateViewModel
//{
//    [Required(ErrorMessage = "Debes seleccionar un usuario.")]
//    [Range(1, int.MaxValue, ErrorMessage = "Debes seleccionar un usuario válido.")]
//    public int UsuarioId { get; set; }

//    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
//    public DateOnly FechaInicio { get; set; } = DateOnly.FromDateTime(DateTime.Today);

//    [Required(ErrorMessage = "La fecha de fin es obligatoria.")]
//    public DateOnly FechaFin { get; set; }

//    [Required(ErrorMessage = "El precio es obligatorio.")]
//    [Range(0.01, 9999, ErrorMessage = "El precio debe ser mayor que 0.")]
//    public decimal Precio { get; set; }

//    [Required(ErrorMessage = "Debes seleccionar un tipo de suscripción.")]
//    [Range(1, int.MaxValue, ErrorMessage = "Debes seleccionar un tipo válido.")]
//    public int TipoSuscripcionId { get; set; }
//}

using System.ComponentModel.DataAnnotations;

namespace FitControlWeb.ViewModels;

public class SuscripcionCreateViewModel
{
    [Required(ErrorMessage = "Debes seleccionar un cliente.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debes seleccionar un cliente válido.")]
    public int UsuarioId { get; set; }

    [Required(ErrorMessage = "Debes seleccionar un tipo de suscripción.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debes seleccionar un tipo válido.")]
    public int TipoSuscripcionId { get; set; }

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    public DateTime FechaInicio { get; set; } = DateTime.Today;
}