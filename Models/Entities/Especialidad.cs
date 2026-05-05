using System;
using System.Collections.Generic;

namespace FitControlWeb.Models.Entities;

public partial class Especialidad
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public bool? Activo { get; set; }

    public DateTime? FechaBaja { get; set; }

    public virtual ICollection<Clase> Clases { get; set; } = new List<Clase>();

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
