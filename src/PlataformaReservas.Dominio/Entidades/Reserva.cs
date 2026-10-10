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

    public Precio PrecioAplicado { get; private set; } = null!;

    public Duracion DuracionAplicada { get; private set; } = null!;

    public NombrePersona NombreCliente { get; private set; } = null!;

    public Telefono TelefonoCliente { get; private set; } = null!;

    public EstadoReserva Estado { get; private set; }

    public Observaciones? Observaciones { get; private set; }

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
        NombrePersona nombreCliente,
        Telefono telefonoCliente,
        Observaciones? observaciones,
        DateTime ahoraUtc)
    {
        Validacion.IdentificadorObligatorio(id, "La reserva");
        Validacion.IdentificadorObligatorio(empresaId, "La empresa de la reserva");
        Validacion.IdentificadorObligatorio(usuarioId, "El usuario de la reserva");
        Validacion.IdentificadorObligatorio(profesionalId, "El profesional de la reserva");
        Validacion.InstanteUtc(ahoraUtc, "La fecha de alta de la reserva");

        if (servicio.EmpresaId != empresaId)
        {
            throw new DominioException("El servicio no pertenece a la empresa de la reserva.");
        }

        FranjaHoraria franja = new(inicioUtc, inicioUtc.AddMinutes(servicio.Duracion.Minutos));

        return new Reserva
        {
            Id = id,
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            ProfesionalId = profesionalId,
            ServicioId = servicio.Id,
            Franja = franja,
            PrecioAplicado = servicio.Precio,
            DuracionAplicada = servicio.Duracion,
            NombreCliente = nombreCliente,
            TelefonoCliente = telefonoCliente,
            Estado = EstadoReserva.Confirmada,
            Observaciones = observaciones,
            FechaAlta = ahoraUtc,
            FechaModificacion = ahoraUtc,
        };
    }

    public void CancelarPorEmpresa(DateTime ahoraUtc)
    {
        Validacion.InstanteUtc(ahoraUtc, "La fecha de cancelacion de la reserva");

        if (Estado != EstadoReserva.Confirmada)
        {
            throw new DominioException("Solo se cancelan reservas confirmadas.");
        }

        Estado = EstadoReserva.Cancelada;
        FechaCancelacion = ahoraUtc;
        FechaModificacion = ahoraUtc;
    }
}
