namespace PlataformaReservas.Aplicacion.Dtos;

public sealed record ServicioDto(
    Guid Id,
    string Nombre,
    string? Descripcion,
    int DuracionMinutos,
    decimal Precio,
    int? IntervaloHuecosMinutos,
    int? AntelacionMinimaMinutos,
    int? AntelacionMaximaDias,
    bool Activo);
