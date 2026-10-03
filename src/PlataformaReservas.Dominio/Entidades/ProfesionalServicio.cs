namespace PlataformaReservas.Dominio.Entidades;

public sealed class ProfesionalServicio
{
    private ProfesionalServicio()
    {
    }

    internal ProfesionalServicio(Guid servicioId)
    {
        ServicioId = servicioId;
    }

    public Guid ServicioId { get; private set; }
}
