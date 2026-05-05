using System;
using System.Collections.Generic;

namespace FitControlWeb.Models.Entities;

public partial class TipoFactura
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Factura> Facturas { get; set; } = new List<Factura>();
}
