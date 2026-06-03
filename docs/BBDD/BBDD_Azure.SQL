/****** Object:  Database [FitControlDB]    Script Date: 03/06/2026 07:38:38 ******/
CREATE DATABASE [FitControlDB]  (EDITION = 'Standard', SERVICE_OBJECTIVE = 'S0', MAXSIZE = 250 GB) WITH CATALOG_COLLATION = DATABASE_DEFAULT, LEDGER = OFF;
GO
ALTER DATABASE [FitControlDB] SET COMPATIBILITY_LEVEL = 150
GO
ALTER DATABASE [FitControlDB] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [FitControlDB] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [FitControlDB] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [FitControlDB] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [FitControlDB] SET ARITHABORT OFF 
GO
ALTER DATABASE [FitControlDB] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [FitControlDB] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [FitControlDB] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [FitControlDB] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [FitControlDB] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [FitControlDB] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [FitControlDB] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [FitControlDB] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [FitControlDB] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [FitControlDB] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [FitControlDB] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [FitControlDB] SET  MULTI_USER 
GO
ALTER DATABASE [FitControlDB] SET ENCRYPTION ON
GO
ALTER DATABASE [FitControlDB] SET QUERY_STORE = ON
GO
ALTER DATABASE [FitControlDB] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 100, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO
/*** Los scripts de las configuraciones con ámbito de base de datos en Azure deben ejecutarse dentro de la conexión de base de datos de destino. ***/
GO
-- ALTER DATABASE SCOPED CONFIGURATION SET MAXDOP = 8;
GO
/****** Object:  Table [dbo].[Auditoria]    Script Date: 03/06/2026 07:38:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Auditoria](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Tabla] [nvarchar](100) NULL,
	[RegistroId] [int] NULL,
	[Accion] [nvarchar](20) NULL,
	[ValoresAntes] [nvarchar](max) NULL,
	[ValoresDespues] [nvarchar](max) NULL,
	[Fecha] [datetime] NULL,
	[UsuarioSistema] [nvarchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Clase]    Script Date: 03/06/2026 07:38:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Clase](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [nvarchar](150) NOT NULL,
	[Fecha] [date] NOT NULL,
	[HoraInicio] [time](7) NOT NULL,
	[HoraFin] [time](7) NOT NULL,
	[CapacidadMinima] [int] NULL,
	[CapacidadMaxima] [int] NULL,
	[EntrenadorId] [int] NOT NULL,
	[EspecialidadId] [int] NOT NULL,
	[Activo] [bit] NULL,
	[FechaBaja] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Conversacion]    Script Date: 03/06/2026 07:38:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Conversacion](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Usuario1Id] [int] NOT NULL,
	[Usuario2Id] [int] NOT NULL,
	[FechaCreacion] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Especialidad]    Script Date: 03/06/2026 07:38:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Especialidad](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [nvarchar](100) NOT NULL,
	[Activo] [bit] NULL,
	[FechaBaja] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Nombre] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[EstadoReserva]    Script Date: 03/06/2026 07:38:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EstadoReserva](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [nvarchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Nombre] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Factura]    Script Date: 03/06/2026 07:38:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Factura](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UsuarioId] [int] NOT NULL,
	[TipoFacturaId] [int] NOT NULL,
	[NumeroFactura] [nvarchar](50) NOT NULL,
	[FechaEmision] [datetime] NULL,
	[Subtotal] [decimal](10, 2) NOT NULL,
	[Impuestos] [decimal](10, 2) NOT NULL,
	[Total] [decimal](10, 2) NOT NULL,
	[Pagada] [bit] NULL,
	[Activo] [bit] NULL,
	[FechaBaja] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[NumeroFactura] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FacturaDetalle]    Script Date: 03/06/2026 07:38:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FacturaDetalle](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FacturaId] [int] NOT NULL,
	[Concepto] [nvarchar](200) NOT NULL,
	[Cantidad] [int] NOT NULL,
	[PrecioUnitario] [decimal](10, 2) NOT NULL,
	[TotalLinea]  AS ([Cantidad]*[PrecioUnitario]) PERSISTED,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Mensaje]    Script Date: 03/06/2026 07:38:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Mensaje](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ConversacionId] [int] NOT NULL,
	[RemitenteId] [int] NOT NULL,
	[Contenido] [nvarchar](max) NOT NULL,
	[FechaEnvio] [datetime] NULL,
	[Leido] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MetodoPago]    Script Date: 03/06/2026 07:38:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MetodoPago](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [nvarchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Nombre] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Pago]    Script Date: 03/06/2026 07:38:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Pago](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FacturaId] [int] NOT NULL,
	[MetodoPagoId] [int] NOT NULL,
	[Monto] [decimal](10, 2) NOT NULL,
	[FechaPago] [datetime] NULL,
	[ReferenciaExterna] [nvarchar](200) NULL,
	[Activo] [bit] NULL,
	[FechaBaja] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Reserva]    Script Date: 03/06/2026 07:38:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Reserva](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UsuarioId] [int] NOT NULL,
	[ClaseId] [int] NOT NULL,
	[EstadoReservaId] [int] NOT NULL,
	[FechaReserva] [datetime] NULL,
	[Activo] [bit] NULL,
	[FechaBaja] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[UsuarioId] ASC,
	[ClaseId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Rol]    Script Date: 03/06/2026 07:38:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Rol](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [nvarchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Nombre] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Suscripcion]    Script Date: 03/06/2026 07:38:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Suscripcion](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UsuarioId] [int] NOT NULL,
	[TipoSuscripcionId] [int] NOT NULL,
	[FechaInicio] [datetime] NOT NULL,
	[FechaFin] [datetime] NOT NULL,
	[Activa] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TipoFactura]    Script Date: 03/06/2026 07:38:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TipoFactura](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [nvarchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Nombre] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TipoSuscripcion]    Script Date: 03/06/2026 07:38:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TipoSuscripcion](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [nvarchar](100) NOT NULL,
	[Precio] [decimal](10, 2) NOT NULL,
	[DuracionDias] [int] NOT NULL,
	[Activo] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Nombre] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Usuario]    Script Date: 03/06/2026 07:38:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Usuario](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Nombre] [nvarchar](100) NOT NULL,
	[Apellidos] [nvarchar](150) NOT NULL,
	[Email] [nvarchar](150) NOT NULL,
	[PasswordHash] [nvarchar](255) NOT NULL,
	[Telefono] [nvarchar](20) NULL,
	[RolId] [int] NOT NULL,
	[FechaRegistro] [datetime] NULL,
	[UltimoLogin] [datetime] NULL,
	[IntentosFallidos] [int] NULL,
	[Bloqueado] [bit] NULL,
	[Activo] [bit] NULL,
	[FechaBaja] [datetime] NULL,
	[RefreshToken] [nvarchar](255) NULL,
	[RefreshTokenExpiryTime] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Email] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UsuarioEspecialidad]    Script Date: 03/06/2026 07:38:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UsuarioEspecialidad](
	[UsuarioId] [int] NOT NULL,
	[EspecialidadId] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[UsuarioId] ASC,
	[EspecialidadId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UsuarioLoginLog]    Script Date: 03/06/2026 07:38:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UsuarioLoginLog](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UsuarioId] [int] NOT NULL,
	[FechaLogin] [datetime] NULL,
	[Exitoso] [bit] NOT NULL,
	[IP] [nvarchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Index [IX_Mensaje_ConversacionId]    Script Date: 03/06/2026 07:38:38 ******/
CREATE NONCLUSTERED INDEX [IX_Mensaje_ConversacionId] ON [dbo].[Mensaje]
(
	[ConversacionId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Pago_FacturaId]    Script Date: 03/06/2026 07:38:38 ******/
CREATE NONCLUSTERED INDEX [IX_Pago_FacturaId] ON [dbo].[Pago]
(
	[FacturaId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Reserva_ClaseId]    Script Date: 03/06/2026 07:38:38 ******/
CREATE NONCLUSTERED INDEX [IX_Reserva_ClaseId] ON [dbo].[Reserva]
(
	[ClaseId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Reserva_UsuarioId]    Script Date: 03/06/2026 07:38:38 ******/
CREATE NONCLUSTERED INDEX [IX_Reserva_UsuarioId] ON [dbo].[Reserva]
(
	[UsuarioId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Auditoria] ADD  DEFAULT (getdate()) FOR [Fecha]
GO
ALTER TABLE [dbo].[Clase] ADD  DEFAULT ((1)) FOR [CapacidadMinima]
GO
ALTER TABLE [dbo].[Clase] ADD  DEFAULT ((50)) FOR [CapacidadMaxima]
GO
ALTER TABLE [dbo].[Clase] ADD  DEFAULT ((1)) FOR [Activo]
GO
ALTER TABLE [dbo].[Conversacion] ADD  DEFAULT (getdate()) FOR [FechaCreacion]
GO
ALTER TABLE [dbo].[Especialidad] ADD  DEFAULT ((1)) FOR [Activo]
GO
ALTER TABLE [dbo].[Factura] ADD  DEFAULT (getdate()) FOR [FechaEmision]
GO
ALTER TABLE [dbo].[Factura] ADD  DEFAULT ((0)) FOR [Pagada]
GO
ALTER TABLE [dbo].[Factura] ADD  DEFAULT ((1)) FOR [Activo]
GO
ALTER TABLE [dbo].[Mensaje] ADD  DEFAULT (getdate()) FOR [FechaEnvio]
GO
ALTER TABLE [dbo].[Mensaje] ADD  DEFAULT ((0)) FOR [Leido]
GO
ALTER TABLE [dbo].[Pago] ADD  DEFAULT (getdate()) FOR [FechaPago]
GO
ALTER TABLE [dbo].[Pago] ADD  DEFAULT ((1)) FOR [Activo]
GO
ALTER TABLE [dbo].[Reserva] ADD  DEFAULT (getdate()) FOR [FechaReserva]
GO
ALTER TABLE [dbo].[Reserva] ADD  DEFAULT ((1)) FOR [Activo]
GO
ALTER TABLE [dbo].[Suscripcion] ADD  DEFAULT ((1)) FOR [Activa]
GO
ALTER TABLE [dbo].[TipoSuscripcion] ADD  DEFAULT ((1)) FOR [Activo]
GO
ALTER TABLE [dbo].[Usuario] ADD  DEFAULT (getdate()) FOR [FechaRegistro]
GO
ALTER TABLE [dbo].[Usuario] ADD  DEFAULT ((0)) FOR [IntentosFallidos]
GO
ALTER TABLE [dbo].[Usuario] ADD  DEFAULT ((0)) FOR [Bloqueado]
GO
ALTER TABLE [dbo].[Usuario] ADD  DEFAULT ((1)) FOR [Activo]
GO
ALTER TABLE [dbo].[UsuarioLoginLog] ADD  DEFAULT (getdate()) FOR [FechaLogin]
GO
ALTER TABLE [dbo].[Clase]  WITH CHECK ADD FOREIGN KEY([EntrenadorId])
REFERENCES [dbo].[Usuario] ([Id])
GO
ALTER TABLE [dbo].[Clase]  WITH CHECK ADD FOREIGN KEY([EspecialidadId])
REFERENCES [dbo].[Especialidad] ([Id])
GO
ALTER TABLE [dbo].[Conversacion]  WITH CHECK ADD FOREIGN KEY([Usuario1Id])
REFERENCES [dbo].[Usuario] ([Id])
GO
ALTER TABLE [dbo].[Conversacion]  WITH CHECK ADD FOREIGN KEY([Usuario2Id])
REFERENCES [dbo].[Usuario] ([Id])
GO
ALTER TABLE [dbo].[Factura]  WITH CHECK ADD FOREIGN KEY([TipoFacturaId])
REFERENCES [dbo].[TipoFactura] ([Id])
GO
ALTER TABLE [dbo].[Factura]  WITH CHECK ADD FOREIGN KEY([UsuarioId])
REFERENCES [dbo].[Usuario] ([Id])
GO
ALTER TABLE [dbo].[FacturaDetalle]  WITH CHECK ADD FOREIGN KEY([FacturaId])
REFERENCES [dbo].[Factura] ([Id])
GO
ALTER TABLE [dbo].[Mensaje]  WITH CHECK ADD FOREIGN KEY([ConversacionId])
REFERENCES [dbo].[Conversacion] ([Id])
GO
ALTER TABLE [dbo].[Mensaje]  WITH CHECK ADD FOREIGN KEY([RemitenteId])
REFERENCES [dbo].[Usuario] ([Id])
GO
ALTER TABLE [dbo].[Pago]  WITH CHECK ADD FOREIGN KEY([FacturaId])
REFERENCES [dbo].[Factura] ([Id])
GO
ALTER TABLE [dbo].[Pago]  WITH CHECK ADD FOREIGN KEY([MetodoPagoId])
REFERENCES [dbo].[MetodoPago] ([Id])
GO
ALTER TABLE [dbo].[Reserva]  WITH CHECK ADD FOREIGN KEY([ClaseId])
REFERENCES [dbo].[Clase] ([Id])
GO
ALTER TABLE [dbo].[Reserva]  WITH CHECK ADD FOREIGN KEY([EstadoReservaId])
REFERENCES [dbo].[EstadoReserva] ([Id])
GO
ALTER TABLE [dbo].[Reserva]  WITH CHECK ADD FOREIGN KEY([UsuarioId])
REFERENCES [dbo].[Usuario] ([Id])
GO
ALTER TABLE [dbo].[Suscripcion]  WITH CHECK ADD FOREIGN KEY([TipoSuscripcionId])
REFERENCES [dbo].[TipoSuscripcion] ([Id])
GO
ALTER TABLE [dbo].[Suscripcion]  WITH CHECK ADD FOREIGN KEY([UsuarioId])
REFERENCES [dbo].[Usuario] ([Id])
GO
ALTER TABLE [dbo].[Usuario]  WITH CHECK ADD FOREIGN KEY([RolId])
REFERENCES [dbo].[Rol] ([Id])
GO
ALTER TABLE [dbo].[UsuarioEspecialidad]  WITH CHECK ADD FOREIGN KEY([EspecialidadId])
REFERENCES [dbo].[Especialidad] ([Id])
GO
ALTER TABLE [dbo].[UsuarioEspecialidad]  WITH CHECK ADD FOREIGN KEY([UsuarioId])
REFERENCES [dbo].[Usuario] ([Id])
GO
ALTER TABLE [dbo].[UsuarioLoginLog]  WITH CHECK ADD FOREIGN KEY([UsuarioId])
REFERENCES [dbo].[Usuario] ([Id])
GO
ALTER TABLE [dbo].[Clase]  WITH CHECK ADD  CONSTRAINT [CHK_Capacidad] CHECK  (([CapacidadMinima]<=[CapacidadMaxima]))
GO
ALTER TABLE [dbo].[Clase] CHECK CONSTRAINT [CHK_Capacidad]
GO
ALTER TABLE [dbo].[Clase]  WITH CHECK ADD  CONSTRAINT [CHK_Horas] CHECK  (([HoraFin]>[HoraInicio]))
GO
ALTER TABLE [dbo].[Clase] CHECK CONSTRAINT [CHK_Horas]
GO
ALTER TABLE [dbo].[Conversacion]  WITH CHECK ADD  CONSTRAINT [CHK_Usuarios_Diferentes] CHECK  (([Usuario1Id]<>[Usuario2Id]))
GO
ALTER TABLE [dbo].[Conversacion] CHECK CONSTRAINT [CHK_Usuarios_Diferentes]
GO
ALTER TABLE [dbo].[Suscripcion]  WITH CHECK ADD  CONSTRAINT [CHK_Fechas] CHECK  (([FechaFin]>[FechaInicio]))
GO
ALTER TABLE [dbo].[Suscripcion] CHECK CONSTRAINT [CHK_Fechas]
GO
/****** Object:  StoredProcedure [dbo].[VerTabla]    Script Date: 03/06/2026 07:38:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[VerTabla]
    @NombreTabla NVARCHAR(100)
AS
BEGIN
    DECLARE @SQL NVARCHAR(MAX);

    SET @SQL = 'SELECT * FROM ' + QUOTENAME(@NombreTabla);

    EXEC sp_executesql @SQL;
END;
GO
ALTER DATABASE [FitControlDB] SET  READ_WRITE 
GO
