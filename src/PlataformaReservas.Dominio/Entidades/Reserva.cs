using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.Eventos;
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
        if (servicio.EmpresaId != empresaId)
        {
            throw new DominioException("El servicio no pertenece a la empresa de la reserva.");
        }

        return Crear(
            id, empresaId, usuarioId, profesionalId, servicio.Id, servicio.Duracion, servicio.Precio,
            inicioUtc, nombreCliente, telefonoCliente, observaciones, ahoraUtc);
    }

    public static Reserva Crear(
        Guid id,
        Guid empresaId,
        Guid usuarioId,
        Guid profesionalId,
        Guid servicioId,
        Duracion duracion,
        Precio precio,
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
        Validacion.IdentificadorObligatorio(servicioId, "El servicio de la reserva");
        Validacion.InstanteUtc(ahoraUtc, "La fecha de alta de la reserva");

        FranjaHoraria franja = new(inicioUtc, inicioUtc.AddMinutes(duracion.Minutos));

        Reserva reserva = new()
        {
            Id = id,
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            ProfesionalId = profesionalId,
            ServicioId = servicioId,
            Franja = franja,
            PrecioAplicado = precio,
            DuracionAplicada = duracion,
            NombreCliente = nombreCliente,
            TelefonoCliente = telefonoCliente,
            Estado = EstadoReserva.Confirmada,
            Observaciones = observaciones,
            FechaAlta = ahoraUtc,
            FechaModificacion = ahoraUtc,
        };
        reserva.RegistrarEvento(new ReservaConfirmada(reserva.Id, ahoraUtc));
        return reserva;
    }

    public void CancelarPorCliente(DateTime ahoraUtc)
    {
        Validacion.InstanteUtc(ahoraUtc, "La fecha de cancelacion de la reserva");

        if (Estado != EstadoReserva.Confirmada)
        {
            throw new DominioException("Solo se cancelan reservas confirmadas.");
        }

        if (Franja.InicioUtc <= ahoraUtc)
        {
            throw new DominioException("La reserva ya ha comenzado.");
        }

        Estado = EstadoReserva.Cancelada;
        FechaCancelacion = ahoraUtc;
        FechaModificacion = ahoraUtc;
        RegistrarEvento(new ReservaCancelada(Id, OrigenCancelacion.Cliente, ahoraUtc));
    }

    public void Completar(DateTime ahoraUtc)
    {
        Validacion.InstanteUtc(ahoraUtc, "La fecha en que se completa la reserva");

        if (Estado != EstadoReserva.Confirmada)
        {
            throw new DominioException("Solo se completan reservas confirmadas.");
        }

        if (Franja.FinUtc >= ahoraUtc)
        {
            throw new DominioException("La reserva aun no ha terminado.");
        }

        Estado = EstadoReserva.Completada;
        FechaModificacion = ahoraUtc;
    }

    public void CancelarPorEmpresa(DateTime ahoraUtc, OrigenCancelacion origen)
    {
        Validacion.InstanteUtc(ahoraUtc, "La fecha de cancelacion de la reserva");

        if (origen is not (OrigenCancelacion.Empresa or OrigenCancelacion.BajaEmpresa))
        {
            throw new DominioException("La empresa solo cancela por decision propia o por su baja.");
        }

        if (Estado != EstadoReserva.Confirmada)
        {
            throw new DominioException("Solo se cancelan reservas confirmadas.");
        }

        Estado = EstadoReserva.Cancelada;
        FechaCancelacion = ahoraUtc;
        FechaModificacion = ahoraUtc;
        RegistrarEvento(new ReservaCancelada(Id, origen, ahoraUtc));
    }
}
