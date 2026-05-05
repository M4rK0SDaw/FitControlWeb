using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace FitControlWeb.ViewModels;

public class UsuarioCreateViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    [StringLength(150, ErrorMessage = "Los apellidos no pueden superar los 150 caracteres.")]
    public string Apellidos { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "Introduce un email válido.")]
    [StringLength(150, ErrorMessage = "El email no puede superar los 150 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    [RegularExpression(@"^[0-9+\s]{6,20}$", ErrorMessage = "El teléfono solo puede contener números, espacios o +.")]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "Debes seleccionar un rol.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debes seleccionar un rol válido.")]
    public int RolId { get; set; }

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 100 caracteres.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public IFormFile? Foto { get; set; }

}