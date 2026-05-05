using System;
using System.Collections.Generic;

namespace FitControlWeb.Models.Entities;

public partial class Factura
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public int TipoFacturaId { get; set; }

    public string NumeroFactura { get; set; } = null!;

    public DateTime? FechaEmision { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Impuestos { get; set; }

    public decimal Total { get; set; }

    public bool? Pagada { get; set; }

    public bool? Activo { get; set; }

    public DateTime? FechaBaja { get; set; }

    public virtual ICollection<FacturaDetalle> FacturaDetalles { get; set; } = new List<FacturaDetalle>();

    public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();

    public virtual TipoFactura TipoFactura { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
