namespace PlataformaReservas.Dominio.Entidades;

// Respaldo interno de Profesional.ServiciosQuePresta. No es una entidad de dominio: no tiene
// identidad propia ni hereda de EntidadBase. Existe porque EF Core no mapea una coleccion de Guid
// a una tabla de union; en base de datos es profesional_servicio con clave (profesional_id, servicio_id).
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
