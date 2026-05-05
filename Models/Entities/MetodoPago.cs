using System;
using System.Collections.Generic;

namespace FitControlWeb.Models.Entities;

public partial class MetodoPago
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}
