using System.ComponentModel.DataAnnotations;

namespace FitControlWeb.ViewModels;

public class EspecialidadCreateViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
}