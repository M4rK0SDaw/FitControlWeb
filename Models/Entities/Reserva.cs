using System;
using System.Collections.Generic;

namespace FitControlWeb.Models.Entities;

public partial class Reserva
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public int ClaseId { get; set; }

    public int EstadoReservaId { get; set; }

    public DateTime? FechaReserva { get; set; }

    public bool? Activo { get; set; }

    public DateTime? FechaBaja { get; set; }

    public virtual Clase Clase { get; set; } = null!;

    public virtual EstadoReserva EstadoReserva { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
