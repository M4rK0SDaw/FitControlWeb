using System;
using System.Collections.Generic;
using FitControlWeb.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FitControlWeb.Data;

public partial class FitControlDbContext : DbContext
{
    public FitControlDbContext()
    {
    }

    public FitControlDbContext(DbContextOptions<FitControlDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Auditorium> Auditoria { get; set; }

    public virtual DbSet<Clase> Clases { get; set; }

    public virtual DbSet<Conversacion> Conversaciones { get; set; }

    public virtual DbSet<Especialidad> Especialidades { get; set; }

    public virtual DbSet<EstadoReserva> EstadoReservas { get; set; }

    public virtual DbSet<Factura> Facturas { get; set; }

    public virtual DbSet<FacturaDetalle> FacturaDetalles { get; set; }

    public virtual DbSet<Mensaje> Mensajes { get; set; }

    public virtual DbSet<MetodoPago> MetodoPagos { get; set; }

    public virtual DbSet<Pago> Pagos { get; set; }

    public virtual DbSet<Reserva> Reservas { get; set; }

    public virtual DbSet<Rol> Rols { get; set; }

    public virtual DbSet<Suscripcion> Suscripciones { get; set; }

    public virtual DbSet<TipoFactura> TipoFacturas { get; set; }

    public virtual DbSet<TipoSuscripcion> TipoSuscripciones { get; set; }
    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<UsuarioLoginLog> UsuarioLoginLogs { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Auditorium>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Auditori__3214EC07FA71314D");

            entity.Property(e => e.Accion).HasMaxLength(20);
            entity.Property(e => e.Fecha)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Tabla).HasMaxLength(100);
            entity.Property(e => e.UsuarioSistema).HasMaxLength(100);
        });

        modelBuilder.Entity<Clase>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Clase__3214EC071D3F1CA4");

            entity.ToTable("Clase");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.CapacidadMaxima).HasDefaultValue(50);
            entity.Property(e => e.CapacidadMinima).HasDefaultValue(1);
            entity.Property(e => e.FechaBaja).HasColumnType("datetime");
            entity.Property(e => e.Nombre).HasMaxLength(150);

            entity.HasOne(d => d.Entrenador).WithMany(p => p.Clases)
                .HasForeignKey(d => d.EntrenadorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Clase__Entrenado__398D8EEE");

            entity.HasOne(d => d.Especialidad).WithMany(p => p.Clases)
                .HasForeignKey(d => d.EspecialidadId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Clase__Especiali__3A81B327");
        });

        modelBuilder.Entity<Conversacion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Conversa__3214EC07543C5A3B");

            entity.ToTable("Conversacion");

            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Usuario1).WithMany(p => p.ConversacionUsuario1s)
                .HasForeignKey(d => d.Usuario1Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Conversac__Usuar__6A30C649");

            entity.HasOne(d => d.Usuario2).WithMany(p => p.ConversacionUsuario2s)
                .HasForeignKey(d => d.Usuario2Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Conversac__Usuar__6B24EA82");
        });

        modelBuilder.Entity<Especialidad>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Especial__3214EC075111C5A6");

            entity.ToTable("Especialidad");

            entity.HasIndex(e => e.Nombre, "UQ__Especial__75E3EFCF622AC64D").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaBaja).HasColumnType("datetime");
            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<EstadoReserva>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EstadoRe__3214EC072032D400");

            entity.ToTable("EstadoReserva");

            entity.HasIndex(e => e.Nombre, "UQ__EstadoRe__75E3EFCFAD120C68").IsUnique();

            entity.Property(e => e.Nombre).HasMaxLength(50);
        });

        modelBuilder.Entity<Factura>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Factura__3214EC07B4D804C7");

            entity.ToTable("Factura");

            entity.HasIndex(e => e.NumeroFactura, "UQ__Factura__CF12F9A6F8ED852A").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaBaja).HasColumnType("datetime");
            entity.Property(e => e.FechaEmision)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Impuestos).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.NumeroFactura).HasMaxLength(50);
            entity.Property(e => e.Pagada).HasDefaultValue(false);
            entity.Property(e => e.Subtotal).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Total).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.TipoFactura).WithMany(p => p.Facturas)
                .HasForeignKey(d => d.TipoFacturaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Factura__TipoFac__5AEE82B9");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Facturas)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Factura__Usuario__59FA5E80");
        });

        modelBuilder.Entity<FacturaDetalle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__FacturaD__3214EC07148D3EBD");

            entity.ToTable("FacturaDetalle");

            entity.Property(e => e.Concepto).HasMaxLength(200);
            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TotalLinea)
                .HasComputedColumnSql("([Cantidad]*[PrecioUnitario])", true)
                .HasColumnType("decimal(21, 2)");

            entity.HasOne(d => d.Factura).WithMany(p => p.FacturaDetalles)
                .HasForeignKey(d => d.FacturaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FacturaDe__Factu__5DCAEF64");
        });

        modelBuilder.Entity<Mensaje>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mensaje__3214EC0742E41364");

            entity.ToTable("Mensaje");

            entity.HasIndex(e => e.ConversacionId, "IX_Mensaje_ConversacionId");

            entity.Property(e => e.FechaEnvio)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Leido).HasDefaultValue(false);

            entity.HasOne(d => d.Conversacion).WithMany(p => p.Mensajes)
                .HasForeignKey(d => d.ConversacionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mensaje__Convers__70DDC3D8");

            entity.HasOne(d => d.Remitente).WithMany(p => p.Mensajes)
                .HasForeignKey(d => d.RemitenteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mensaje__Remiten__71D1E811");
        });

        modelBuilder.Entity<MetodoPago>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MetodoPa__3214EC075D6435B4");

            entity.ToTable("MetodoPago");

            entity.HasIndex(e => e.Nombre, "UQ__MetodoPa__75E3EFCF4DFB9BA6").IsUnique();

            entity.Property(e => e.Nombre).HasMaxLength(50);
        });

        modelBuilder.Entity<Pago>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Pago__3214EC07222426CD");

            entity.ToTable("Pago");

            entity.HasIndex(e => e.FacturaId, "IX_Pago_FacturaId");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaBaja).HasColumnType("datetime");
            entity.Property(e => e.FechaPago)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Monto).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ReferenciaExterna).HasMaxLength(200);

            entity.HasOne(d => d.Factura).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.FacturaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Pago__FacturaId__656C112C");

            entity.HasOne(d => d.MetodoPago).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.MetodoPagoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Pago__MetodoPago__66603565");
        });

        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Reserva__3214EC0700DDA52C");

            entity.ToTable("Reserva");

            entity.HasIndex(e => e.ClaseId, "IX_Reserva_ClaseId");

            entity.HasIndex(e => e.UsuarioId, "IX_Reserva_UsuarioId");

            entity.HasIndex(e => new { e.UsuarioId, e.ClaseId }, "UQ__Reserva__0469CEECD87A2308").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaBaja).HasColumnType("datetime");
            entity.Property(e => e.FechaReserva)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Clase).WithMany(p => p.Reservas)
                .HasForeignKey(d => d.ClaseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reserva__ClaseId__45F365D3");

            entity.HasOne(d => d.EstadoReserva).WithMany(p => p.Reservas)
                .HasForeignKey(d => d.EstadoReservaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reserva__EstadoR__46E78A0C");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Reservas)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reserva__Usuario__44FF419A");
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Rol__3214EC07AF73B8A0");

            entity.ToTable("Rol");

            entity.HasIndex(e => e.Nombre, "UQ__Rol__75E3EFCF1FF344DA").IsUnique();

            entity.Property(e => e.Nombre).HasMaxLength(50);
        });

        modelBuilder.Entity<Suscripcion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Suscripc__3214EC07B835B29E");

            entity.ToTable("Suscripcion");

            entity.Property(e => e.Activa).HasDefaultValue(true);
            entity.Property(e => e.FechaFin).HasColumnType("datetime");
            entity.Property(e => e.FechaInicio).HasColumnType("datetime");

            entity.HasOne(d => d.TipoSuscripcion).WithMany(p => p.Suscripcions)
                .HasForeignKey(d => d.TipoSuscripcionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Suscripci__TipoS__4F7CD00D");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Suscripcions)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Suscripci__Usuar__4E88ABD4");
        });

        modelBuilder.Entity<TipoFactura>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TipoFact__3214EC07D4B551C7");

            entity.ToTable("TipoFactura");

            entity.HasIndex(e => e.Nombre, "UQ__TipoFact__75E3EFCFC21234A0").IsUnique();

            entity.Property(e => e.Nombre).HasMaxLength(50);
        });

        modelBuilder.Entity<TipoSuscripcion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TipoSusc__3214EC07FF35E3C8");

            entity.ToTable("TipoSuscripcion");

            entity.HasIndex(e => e.Nombre, "UQ__TipoSusc__75E3EFCFAFD072FF").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Precio).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Usuario__3214EC0776AC0B54");

            entity.ToTable("Usuario");

            entity.HasIndex(e => e.Email, "UQ__Usuario__A9D10534487DAA80").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Apellidos).HasMaxLength(150);
            entity.Property(e => e.Bloqueado).HasDefaultValue(false);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.FechaBaja).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IntentosFallidos).HasDefaultValue(0);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.RefreshToken).HasMaxLength(255);
            entity.Property(e => e.RefreshTokenExpiryTime).HasColumnType("datetime");
            entity.Property(e => e.Telefono).HasMaxLength(20);
            entity.Property(e => e.UltimoLogin).HasColumnType("datetime");

            entity.HasOne(d => d.Rol).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.RolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Usuario__RolId__2C3393D0");

            entity.HasMany(d => d.Especialidads).WithMany(p => p.Usuarios)
                .UsingEntity<Dictionary<string, object>>(
                    "UsuarioEspecialidad",
                    r => r.HasOne<Especialidad>().WithMany()
                        .HasForeignKey("EspecialidadId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UsuarioEs__Espec__33D4B598"),
                    l => l.HasOne<Usuario>().WithMany()
                        .HasForeignKey("UsuarioId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UsuarioEs__Usuar__32E0915F"),
                    j =>
                    {
                        j.HasKey("UsuarioId", "EspecialidadId").HasName("PK__UsuarioE__24AD963BAE020359");
                        j.ToTable("UsuarioEspecialidad");
                    });
        });

        modelBuilder.Entity<UsuarioLoginLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UsuarioL__3214EC075D243FD0");

            entity.ToTable("UsuarioLoginLog");

            entity.Property(e => e.FechaLogin)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Ip)
                .HasMaxLength(50)
                .HasColumnName("IP");

            entity.HasOne(d => d.Usuario).WithMany(p => p.UsuarioLoginLogs)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UsuarioLo__Usuar__787EE5A0");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
