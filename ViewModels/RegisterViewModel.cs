using System.ComponentModel.DataAnnotations;

namespace FitControlWeb.ViewModels;

public class RegisterViewModel
{
    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    public string Apellidos { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? Telefono { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}