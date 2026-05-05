using System;
using System.Collections.Generic;

namespace FitControlWeb.Models.Entities;

public partial class Auditorium
{
    public int Id { get; set; }

    public string? Tabla { get; set; }

    public int? RegistroId { get; set; }

    public string? Accion { get; set; }

    public string? ValoresAntes { get; set; }

    public string? ValoresDespues { get; set; }

    public DateTime? Fecha { get; set; }

    public string? UsuarioSistema { get; set; }
}
