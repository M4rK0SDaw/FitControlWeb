using System;
using System.Collections.Generic;

namespace FitControlWeb.Models.Entities;

public partial class Conversacion
{
    public int Id { get; set; }

    public int Usuario1Id { get; set; }

    public int Usuario2Id { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public virtual ICollection<Mensaje> Mensajes { get; set; } = new List<Mensaje>();

    public virtual Usuario Usuario1 { get; set; } = null!;

    public virtual Usuario Usuario2 { get; set; } = null!;
}
