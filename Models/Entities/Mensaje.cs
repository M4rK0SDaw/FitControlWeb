using System;
using System.Collections.Generic;

namespace FitControlWeb.Models.Entities;

public partial class Mensaje
{
    public int Id { get; set; }

    public int ConversacionId { get; set; }

    public int RemitenteId { get; set; }

    public string Contenido { get; set; } = null!;

    public DateTime? FechaEnvio { get; set; }

    public bool? Leido { get; set; }

    public virtual Conversacion Conversacion { get; set; } = null!;

    public virtual Usuario Remitente { get; set; } = null!;
}
