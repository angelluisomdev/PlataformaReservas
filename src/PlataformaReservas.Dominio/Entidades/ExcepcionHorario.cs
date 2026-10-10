using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Dominio.Entidades;

public sealed class ExcepcionHorario : EntidadBase
{
    private ExcepcionHorario()
    {
    }

    public Guid EmpresaId { get; private set; }

    public Guid ProfesionalId { get; private set; }

    public DateOnly Fecha { get; private set; }

    public MotivoExcepcion? Motivo { get; private set; }

    internal static ExcepcionHorario Crear(Guid id, Guid empresaId, Guid profesionalId, DateOnly fecha, MotivoExcepcion? motivo)
    {
        Validacion.IdentificadorObligatorio(id, "La excepcion de horario");

        return new ExcepcionHorario
        {
            Id = id,
            EmpresaId = empresaId,
            ProfesionalId = profesionalId,
            Fecha = fecha,
            Motivo = motivo,
        };
    }
}
