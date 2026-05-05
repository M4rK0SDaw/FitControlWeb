using System;
using System.Collections.Generic;

namespace FitControlWeb.Models.Entities;

public partial class Pago
{
    public int Id { get; set; }

    public int FacturaId { get; set; }

    public int MetodoPagoId { get; set; }

    public decimal Monto { get; set; }

    public DateTime? FechaPago { get; set; }

    public string? ReferenciaExterna { get; set; }

    public bool? Activo { get; set; }

    public DateTime? FechaBaja { get; set; }

    public virtual Factura Factura { get; set; } = null!;

    public virtual MetodoPago MetodoPago { get; set; } = null!;
}
