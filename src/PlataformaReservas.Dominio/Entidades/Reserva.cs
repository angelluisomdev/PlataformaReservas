using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Dominio.Entidades;

public sealed class Reserva : EntidadBase
{
    private Reserva()
    {
    }

    public Guid EmpresaId { get; private set; }

    public Guid UsuarioId { get; private set; }

    public Guid ProfesionalId { get; private set; }

    public Guid ServicioId { get; private set; }

    public FranjaHoraria Franja { get; private set; } = null!;

    public decimal PrecioAplicado { get; private set; }

    public int DuracionAplicadaMinutos { get; private set; }

    public string NombreCliente { get; private set; } = null!;

    public string TelefonoCliente { get; private set; } = null!;

    public EstadoReserva Estado { get; private set; }

    public string? Observaciones { get; private set; }

    public DateTime FechaAlta { get; private set; }

    public DateTime FechaModificacion { get; private set; }

    public DateTime? FechaCancelacion { get; private set; }

    public static Reserva Crear(
        Guid id,
        Guid empresaId,
        Guid usuarioId,
        Guid profesionalId,
        Servicio servicio,
        DateTime inicioUtc,
        string nombreCliente,
        string telefonoCliente,
        string? observaciones,
        DateTime ahoraUtc)
    {
        Validacion.IdentificadorObligatorio(id, "La reserva");
        Validacion.IdentificadorObligatorio(empresaId, "La empresa de la reserva");
        Validacion.IdentificadorObligatorio(usuarioId, "El usuario de la reserva");
        Validacion.IdentificadorObligatorio(profesionalId, "El profesional de la reserva");
        Validacion.TextoObligatorio(nombreCliente, 120, "El nombre del cliente");
        Validacion.TextoObligatorio(telefonoCliente, 20, "El telefono del cliente");
        Validacion.TextoOpcional(observaciones, 500, "Las observaciones");
        Validacion.InstanteUtc(ahoraUtc, "La fecha de alta de la reserva");

        if (servicio.EmpresaId != empresaId)
        {
            throw new DominioException("El servicio no pertenece a la empresa de la reserva.");
        }

        FranjaHoraria franja = new(inicioUtc, inicioUtc.AddMinutes(servicio.DuracionMinutos));

        return new Reserva
        {
            Id = id,
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            ProfesionalId = profesionalId,
            ServicioId = servicio.Id,
            Franja = franja,
            PrecioAplicado = servicio.Precio,
            DuracionAplicadaMinutos = servicio.DuracionMinutos,
            NombreCliente = nombreCliente.Trim(),
            TelefonoCliente = telefonoCliente.Trim(),
            Estado = EstadoReserva.Confirmada,
            Observaciones = observaciones,
            FechaAlta = ahoraUtc,
            FechaModificacion = ahoraUtc,
        };
    }
}
