namespace PlataformaReservas.Aplicacion.Dtos;

public sealed record ConfiguracionEmpresaDto(
    string Nombre,
    string Slug,
    Guid CategoriaId,
    string? Descripcion,
    string Email,
    string Telefono,
    string Direccion,
    string CodigoPostal,
    string Ciudad,
    int IntervaloHuecosMinutos,
    int AntelacionMinimaMinutos,
    int AntelacionMaximaDias,
    bool Activa,
    int ReservasFuturas);
