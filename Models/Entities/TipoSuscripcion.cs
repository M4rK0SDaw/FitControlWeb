using System;
using System.Collections.Generic;

namespace FitControlWeb.Models.Entities;

public partial class TipoSuscripcion
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public decimal Precio { get; set; }

    public int DuracionDias { get; set; }

    public bool? Activo { get; set; }

    public virtual ICollection<Suscripcion> Suscripcions { get; set; } = new List<Suscripcion>();
}
