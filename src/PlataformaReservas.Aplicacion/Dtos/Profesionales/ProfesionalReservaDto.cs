using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Aplicacion.Dtos.Profesionales;

public sealed record ProfesionalReservaDto(Guid Id, string Nombre)
{
    public static ProfesionalReservaDto ConvertirADto(Profesional profesional)
    {
        return new ProfesionalReservaDto(profesional.Id, profesional.NombreCompleto.Valor);
    }
}
