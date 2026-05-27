# Base de datos FitControl Web

## Diagrama entidad-relación

```mermaid
erDiagram
    ROL ||--o{ USUARIO : tiene
    USUARIO ||--o{ CLASE : imparte
    ESPECIALIDAD ||--o{ CLASE : clasifica
    USUARIO ||--o{ RESERVA : realiza
    CLASE ||--o{ RESERVA : recibe
    ESTADORESERVA ||--o{ RESERVA : estado
    USUARIO ||--o{ SUSCRIPCION : contrata
    TIPOSUSCRIPCION ||--o{ SUSCRIPCION : define
    USUARIO ||--o{ FACTURA : posee
    TIPOFACTURA ||--o{ FACTURA : categoriza
    FACTURA ||--o{ FACTURADETALLE : contiene
    FACTURA ||--o{ PAGO : registra
    METODOPAGO ||--o{ PAGO : usa
    CONVERSACION ||--o{ MENSAJE : contiene
    USUARIO ||--o{ MENSAJE : envia
    USUARIO ||--o{ CONVERSACION : participa
    USUARIO ||--o{ USUARIOESPECIALIDAD : relaciona
    ESPECIALIDAD ||--o{ USUARIOESPECIALIDAD : relaciona

    ROL {
        int Id PK
        string Nombre
    }
    USUARIO {
        int Id PK
        string Nombre
        string Apellidos
        string Email
        string PasswordHash
        string Telefono
        int RolId FK
        bool Activo
        bool Bloqueado
        datetime FechaRegistro
    }
    ESPECIALIDAD {
        int Id PK
        string Nombre
        bool Activo
    }
    CLASE {
        int Id PK
        string Nombre
        date Fecha
        time HoraInicio
        time HoraFin
        int CapacidadMaxima
        int EntrenadorId FK
        int EspecialidadId FK
        bool Activo
    }
    ESTADORESERVA {
        int Id PK
        string Nombre
    }
    RESERVA {
        int Id PK
        int UsuarioId FK
        int ClaseId FK
        int EstadoReservaId FK
        datetime FechaReserva
        bool Activo
    }
    TIPOSUSCRIPCION {
        int Id PK
        string Nombre
        decimal Precio
        int DuracionDias
        bool Activo
    }
    SUSCRIPCION {
        int Id PK
        int UsuarioId FK
        int TipoSuscripcionId FK
        datetime FechaInicio
        datetime FechaFin
        bool Activa
    }
    TIPOFACTURA {
        int Id PK
        string Nombre
    }
    FACTURA {
        int Id PK
        int UsuarioId FK
        int TipoFacturaId FK
        string NumeroFactura
        datetime FechaEmision
        decimal Subtotal
        decimal Impuestos
        decimal Total
        bool Pagada
    }
    FACTURADETALLE {
        int Id PK
        int FacturaId FK
        string Concepto
        int Cantidad
        decimal PrecioUnitario
    }
    METODOPAGO {
        int Id PK
        string Nombre
    }
    PAGO {
        int Id PK
        int FacturaId FK
        int MetodoPagoId FK
        decimal Monto
        datetime FechaPago
        string ReferenciaExterna
    }
    CONVERSACION {
        int Id PK
        int Usuario1Id FK
        int Usuario2Id FK
        datetime FechaCreacion
    }
    MENSAJE {
        int Id PK
        int ConversacionId FK
        int RemitenteId FK
        string Contenido
        datetime FechaEnvio
        bool Leido
    }
    USUARIOESPECIALIDAD {
        int UsuarioId PK
        int EspecialidadId PK
    }
```

## Tablas principales

- **Usuario**: almacena los datos de acceso, perfil, rol, estado de cuenta, bloqueos y tokens de recuperación.
- **Rol**: define los perfiles del sistema, principalmente administrador, entrenador y cliente.
- **Clase**: representa cada sesión deportiva, indicando entrenador, especialidad, horario, capacidad y estado.
- **Reserva**: registra la inscripción de un cliente en una clase concreta.
- **EstadoReserva**: normaliza el estado funcional de cada reserva, como activa, cancelada o equivalente.
- **Suscripcion**: guarda la contratación de un plan por parte del cliente con su rango de fechas.
- **TipoSuscripcion**: define el catálogo de planes disponibles, precio, duración y activación.
- **Factura**: recoge la cabecera económica del documento emitido al usuario.
- **FacturaDetalle**: desglosa conceptos, cantidades y precio unitario de cada factura.
- **Pago**: registra los pagos asociados a una factura, incluyendo método e identificador externo.
- **MetodoPago**: catálogo de medios de pago admitidos, por ejemplo Stripe o pago manual.
- **Conversacion**: representa un canal de comunicación privado entre dos usuarios.
- **Mensaje**: contiene cada texto enviado dentro de una conversación, junto con fecha y estado de lectura.

## Nota técnica

Además de las tablas anteriores, el proyecto incorpora tablas auxiliares como `TipoFactura`, `UsuarioEspecialidad`, `UsuarioLoginLog` y `Auditoria`, que refuerzan la trazabilidad, la gestión interna y la organización funcional del sistema.
