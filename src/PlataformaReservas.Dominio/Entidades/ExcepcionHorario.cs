using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.Entidades;

public sealed class ExcepcionHorario : EntidadBase
{
    private ExcepcionHorario()
    {
    }

    public Guid EmpresaId { get; private set; }

    public Guid ProfesionalId { get; private set; }

    public DateOnly Fecha { get; private set; }

    public string? Motivo { get; private set; }
}
