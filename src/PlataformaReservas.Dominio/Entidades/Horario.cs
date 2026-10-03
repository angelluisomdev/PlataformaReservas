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
}
