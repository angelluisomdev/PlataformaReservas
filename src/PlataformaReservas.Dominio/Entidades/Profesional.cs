using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Dominio.Entidades;

public sealed class Profesional : EntidadBase
{
    private readonly List<Horario> _horarios = [];
    private readonly List<ExcepcionHorario> _excepciones = [];
    private readonly List<ProfesionalServicio> _servicios = [];

    private Profesional()
    {
    }

    public Guid EmpresaId { get; private set; }

    public NombrePersona NombreCompleto { get; private set; } = null!;

    public Email? Email { get; private set; }

    public Telefono? Telefono { get; private set; }

    public bool Activo { get; private set; }

    public DateTime FechaAlta { get; private set; }

    public IReadOnlyCollection<Horario> Horarios => _horarios.AsReadOnly();

    public IReadOnlyCollection<ExcepcionHorario> Excepciones => _excepciones.AsReadOnly();

    public IReadOnlySet<Guid> ServiciosQuePresta => _servicios.Select(s => s.ServicioId).ToHashSet();

    public static Profesional Crear(Guid id, Guid empresaId, NombrePersona nombreCompleto, DateTime ahoraUtc)
    {
        Validacion.IdentificadorObligatorio(id, "El profesional");
        Validacion.IdentificadorObligatorio(empresaId, "La empresa del profesional");
        Validacion.InstanteUtc(ahoraUtc, "La fecha de alta del profesional");

        return new Profesional
        {
            Id = id,
            EmpresaId = empresaId,
            NombreCompleto = nombreCompleto,
            Activo = true,
            FechaAlta = ahoraUtc,
        };
    }

    public void ActualizarDatos(NombrePersona nombreCompleto, Email? email, Telefono? telefono)
    {
        NombreCompleto = nombreCompleto;
        Email = email;
        Telefono = telefono;
    }

    public void Desactivar()
    {
        if (!Activo)
        {
            throw new DominioException("El profesional ya esta desactivado.");
        }

        Activo = false;
    }

    public bool Presta(Guid servicioId)
    {
        return _servicios.Exists(s => s.ServicioId == servicioId);
    }

    public void AsociarServicio(Guid servicioId)
    {
        Validacion.IdentificadorObligatorio(servicioId, "El servicio asociado");

        if (Presta(servicioId))
        {
            throw new DominioException("El profesional ya presta ese servicio.");
        }

        _servicios.Add(new ProfesionalServicio(servicioId));
    }

    public void DesasociarServicio(Guid servicioId)
    {
        if (_servicios.RemoveAll(s => s.ServicioId == servicioId) == 0)
        {
            throw new DominioException("El profesional no presta ese servicio.");
        }
    }

    public void AgregarIntervalo(Guid horarioId, DayOfWeek dia, IntervaloHorario intervalo)
    {
        Horario? solapado = _horarios.Find(h => h.DiaSemana == dia && h.Intervalo.SeSolapaCon(intervalo));
        if (solapado is not null)
        {
            throw new DominioException(
                $"El intervalo se solapa con el de {solapado.Intervalo.HoraInicio:HH\\:mm} a {solapado.Intervalo.HoraFin:HH\\:mm} del mismo dia.");
        }

        _horarios.Add(Horario.Crear(horarioId, EmpresaId, Id, dia, intervalo));
    }

    public void EliminarIntervalo(Guid horarioId)
    {
        if (_horarios.RemoveAll(h => h.Id == horarioId) == 0)
        {
            throw new DominioException("El profesional no tiene ese intervalo de horario.");
        }
    }

    public void MarcarNoDisponible(Guid excepcionId, DateOnly fecha, MotivoExcepcion? motivo)
    {
        if (_excepciones.Exists(x => x.Fecha == fecha))
        {
            throw new DominioException("Ya hay una excepcion para ese profesional en esa fecha.");
        }

        _excepciones.Add(ExcepcionHorario.Crear(excepcionId, EmpresaId, Id, fecha, motivo));
    }

    public void QuitarExcepcion(DateOnly fecha)
    {
        if (_excepciones.RemoveAll(x => x.Fecha == fecha) == 0)
        {
            throw new DominioException("El profesional no tiene una excepcion en esa fecha.");
        }
    }
}
