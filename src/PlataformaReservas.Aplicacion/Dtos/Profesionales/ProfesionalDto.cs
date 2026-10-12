using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Aplicacion.Dtos.Profesionales;

public sealed record ProfesionalDto(
    Guid Id, string NombreCompleto, string? Email, string? Telefono, bool Activo, IReadOnlyCollection<Guid> Servicios)
{
    public static ProfesionalDto ConvertirADto(Profesional profesional)
    {
        return new ProfesionalDto(
            profesional.Id,
            profesional.NombreCompleto.Valor,
            profesional.Email?.Valor,
            profesional.Telefono?.Valor,
            profesional.Activo,
            profesional.ServiciosQuePresta.ToList());
    }
}
