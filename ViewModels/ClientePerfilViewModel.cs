using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace FitControlWeb.ViewModels;

public class ClientePerfilViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    [StringLength(150)]
    public string Apellidos { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    [RegularExpression(@"^[0-9+\s]{6,20}$", ErrorMessage = "El teléfono solo puede contener números, espacios o +.")]
    public string? Telefono { get; set; }

    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 100 caracteres.")]
    public string? NuevaPassword { get; set; }

    public IFormFile? Foto { get; set; }
}