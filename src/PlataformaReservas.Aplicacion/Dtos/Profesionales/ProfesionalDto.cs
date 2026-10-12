namespace PlataformaReservas.Aplicacion.Dtos;

public sealed record ProfesionalDto(
    Guid Id, string NombreCompleto, string? Email, string? Telefono, bool Activo, IReadOnlyCollection<Guid> Servicios);
