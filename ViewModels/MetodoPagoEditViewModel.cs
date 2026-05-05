using System.ComponentModel.DataAnnotations;

namespace FitControlWeb.ViewModels;

public class MetodoPagoEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "Máximo 50 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
}