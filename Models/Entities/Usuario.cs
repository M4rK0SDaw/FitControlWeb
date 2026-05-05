using System;
using System.Collections.Generic;

namespace FitControlWeb.Models.Entities;

public partial class Usuario
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Apellidos { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Telefono { get; set; }

    public int RolId { get; set; }

    public DateTime? FechaRegistro { get; set; }

    public DateTime? UltimoLogin { get; set; }

    public int? IntentosFallidos { get; set; }

    public bool? Bloqueado { get; set; }

    public bool? Activo { get; set; }

    public DateTime? FechaBaja { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiryTime { get; set; }

    public virtual ICollection<Clase> Clases { get; set; } = new List<Clase>();

    public virtual ICollection<Conversacion> ConversacionUsuario1s { get; set; } = new List<Conversacion>();

    public virtual ICollection<Conversacion> ConversacionUsuario2s { get; set; } = new List<Conversacion>();

    public virtual ICollection<Factura> Facturas { get; set; } = new List<Factura>();

    public virtual ICollection<Mensaje> Mensajes { get; set; } = new List<Mensaje>();

    public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();

    public virtual Rol Rol { get; set; } = null!;

    public virtual ICollection<Suscripcion> Suscripcions { get; set; } = new List<Suscripcion>();

    public virtual ICollection<UsuarioLoginLog> UsuarioLoginLogs { get; set; } = new List<UsuarioLoginLog>();

    public virtual ICollection<Especialidad> Especialidads { get; set; } = new List<Especialidad>();
}
