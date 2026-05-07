using System.ComponentModel.DataAnnotations;

namespace FitControlWeb.ViewModels.Auth;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "Introduce un email válido.")]
    public string Email { get; set; } = string.Empty;
}