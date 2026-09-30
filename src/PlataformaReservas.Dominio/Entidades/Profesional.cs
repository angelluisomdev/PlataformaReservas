using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.Entidades;

// Raiz del agregado que contiene sus horarios, sus excepciones y los servicios que presta:
// RN-42 y RN-45 solo se pueden garantizar viendolos todos juntos (modelo-dominio.md §2.1).
public sealed class Profesional : EntidadBase
{
    private readonly List<Horario> _horarios = [];
    private readonly List<ExcepcionHorario> _excepciones = [];
    private readonly List<ProfesionalServicio> _servicios = [];

    private Profesional()
    {
    }

    // Se fija al crear y ningun metodo lo cambia (RN-01).
    public Guid EmpresaId { get; private set; }

    public string NombreCompleto { get; private set; } = null!;

    public string? Email { get; private set; }

    public string? Telefono { get; private set; }

    public bool Activo { get; private set; }

    public DateTime FechaAlta { get; private set; }

    public IReadOnlyCollection<Horario> Horarios => _horarios.AsReadOnly();

    public IReadOnlyCollection<ExcepcionHorario> Excepciones => _excepciones.AsReadOnly();

    public IReadOnlySet<Guid> ServiciosQuePresta => _servicios.Select(s => s.ServicioId).ToHashSet();

    public static Profesional Crear(Guid id, Guid empresaId, string nombreCompleto, DateTime ahoraUtc)
    {
        Validacion.IdentificadorObligatorio(id, "El profesional");
        Validacion.IdentificadorObligatorio(empresaId, "La empresa del profesional");
        Validacion.TextoObligatorio(nombreCompleto, 120, "El nombre del profesional");
        Validacion.InstanteUtc(ahoraUtc, "La fecha de alta del profesional");

        return new Profesional
        {
            Id = id,
            EmpresaId = empresaId,
            NombreCompleto = nombreCompleto.Trim(),
            Activo = true,
            FechaAlta = ahoraUtc,
        };
    }
}
