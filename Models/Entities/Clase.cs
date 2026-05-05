using System;
using System.Collections.Generic;

namespace FitControlWeb.Models.Entities;

public partial class Clase
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public DateOnly Fecha { get; set; }

    public TimeOnly HoraInicio { get; set; }

    public TimeOnly HoraFin { get; set; }

    public int? CapacidadMinima { get; set; }

    public int? CapacidadMaxima { get; set; }

    public int EntrenadorId { get; set; }

    public int EspecialidadId { get; set; }

    public bool? Activo { get; set; }

    public DateTime? FechaBaja { get; set; }

    public virtual Usuario Entrenador { get; set; } = null!;

    public virtual Especialidad Especialidad { get; set; } = null!;

    public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
