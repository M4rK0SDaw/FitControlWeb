using System;
using System.Collections.Generic;

namespace FitControlWeb.Models.Entities;

public partial class UsuarioLoginLog
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public DateTime? FechaLogin { get; set; }

    public bool Exitoso { get; set; }

    public string? Ip { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
