using System.ComponentModel.DataAnnotations;

namespace FitControlWeb.ViewModels;

public class ExportButtonsViewModel
{
    [Required(ErrorMessage = "El nombre de la clase es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha es obligatoria.")]
    public DateOnly Fecha { get; set; }

    [Required(ErrorMessage = "La hora de inicio es obligatoria.")]
    public TimeOnly HoraInicio { get; set; }

    [Required(ErrorMessage = "La hora de fin es obligatoria.")]
    public TimeOnly HoraFin { get; set; }

    [Range(1, 50, ErrorMessage = "La capacidad mínima debe estar entre 1 y 50.")]
    public int CapacidadMinima { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "La capacidad máxima debe estar entre 1 y 100.")]
    public int CapacidadMaxima { get; set; } = 50;

    [Required(ErrorMessage = "Debes seleccionar un entrenador.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debes seleccionar un entrenador válido.")]
    public int EntrenadorId { get; set; }

    [Required(ErrorMessage = "Debes seleccionar una especialidad.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debes seleccionar una especialidad válida.")]
    public int EspecialidadId { get; set; }
}