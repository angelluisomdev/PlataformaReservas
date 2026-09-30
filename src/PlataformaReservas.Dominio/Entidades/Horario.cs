using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Dominio.Entidades;

// Hijo del agregado Profesional: nace y muere con sus llamadas y no tiene repositorio (modelo-dominio.md §4.7).
// Se crea desde Profesional.AgregarIntervalo, en la Fase 9.
public sealed class Horario : EntidadBase
{
    private Horario()
    {
    }

    // Desnormalizado desde el profesional para poder filtrar sin join (modelo-dominio.md §2.5).
    public Guid EmpresaId { get; private set; }

    public Guid ProfesionalId { get; private set; }

    public DayOfWeek DiaSemana { get; private set; }

    public IntervaloHorario Intervalo { get; private set; } = null!;
}
