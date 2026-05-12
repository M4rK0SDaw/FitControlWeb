using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace FitControlWeb.ViewModels;

public class UsuarioEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    [StringLength(150, ErrorMessage = "Los apellidos no pueden superar los 150 caracteres.")]
    public string Apellidos { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "Introduce un email valido.")]
    [StringLength(150, ErrorMessage = "El email no puede superar los 150 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "El telefono no puede superar los 20 caracteres.")]
    [RegularExpression(@"^[0-9+\s]{6,20}$", ErrorMessage = "El telefono solo puede contener numeros, espacios o +.")]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "Debes seleccionar un rol.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debes seleccionar un rol valido.")]
    public int RolId { get; set; }

    public bool Activo { get; set; }

    [StringLength(100, MinimumLength = 6, ErrorMessage = "La nueva contrasena debe tener entre 6 y 100 caracteres.")]
    [DataType(DataType.Password)]
    public string? NuevaPassword { get; set; }

    public IFormFile? Foto { get; set; }
    public List<SelectListItem> Roles { get; set; } = new();
}
