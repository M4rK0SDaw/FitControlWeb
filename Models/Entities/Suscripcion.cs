using System;
using System.Collections.Generic;

namespace FitControlWeb.Models.Entities;

public partial class Suscripcion
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public int TipoSuscripcionId { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public bool? Activa { get; set; }

    public virtual TipoSuscripcion TipoSuscripcion { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;

}
