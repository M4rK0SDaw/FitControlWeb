using System.ComponentModel.DataAnnotations;

namespace FitControlWeb.ViewModels;

public class TipoSuscripcionCreateViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(0.01, 9999, ErrorMessage = "El precio debe ser mayor que 0.")]
    public decimal Precio { get; set; }

    [Required(ErrorMessage = "La duración es obligatoria.")]
    [Range(1, 3650, ErrorMessage = "La duración debe estar entre 1 y 3650 días.")]
    public int DuracionDias { get; set; }
}