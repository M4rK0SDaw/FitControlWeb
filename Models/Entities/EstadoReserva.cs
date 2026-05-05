using System;
using System.Collections.Generic;

namespace FitControlWeb.Models.Entities;

public partial class EstadoReserva
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
