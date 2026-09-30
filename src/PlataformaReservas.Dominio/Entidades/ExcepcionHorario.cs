using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.Entidades;

// Indisponibilidad de dia completo de un profesional (RN-44). Hijo del agregado Profesional.
// Se crea desde Profesional.MarcarNoDisponible, en la Fase 9.
public sealed class ExcepcionHorario : EntidadBase
{
    private ExcepcionHorario()
    {
    }

    // Desnormalizado desde el profesional para poder filtrar sin join (modelo-dominio.md §2.5).
    public Guid EmpresaId { get; private set; }

    public Guid ProfesionalId { get; private set; }

    // Dia local de la empresa, sin conversion (RN-101).
    public DateOnly Fecha { get; private set; }

    public string? Motivo { get; private set; }
}
