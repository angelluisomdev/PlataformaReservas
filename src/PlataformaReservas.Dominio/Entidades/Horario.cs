using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Dominio.Entidades;

public sealed class Horario : EntidadBase
{
    private Horario()
    {
    }

    public Guid EmpresaId { get; private set; }

    public Guid ProfesionalId { get; private set; }

    public DayOfWeek DiaSemana { get; private set; }

    public IntervaloHorario Intervalo { get; private set; } = null!;

    internal static Horario Crear(Guid id, Guid empresaId, Guid profesionalId, DayOfWeek diaSemana, IntervaloHorario intervalo)
    {
        Validacion.IdentificadorObligatorio(id, "El horario");

        return new Horario
        {
            Id = id,
            EmpresaId = empresaId,
            ProfesionalId = profesionalId,
            DiaSemana = diaSemana,
            Intervalo = intervalo,
        };
    }
}
