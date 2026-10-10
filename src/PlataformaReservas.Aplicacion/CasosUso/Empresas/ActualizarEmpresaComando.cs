namespace PlataformaReservas.Aplicacion.CasosUso.Empresas;

public sealed record ActualizarEmpresaComando(
    string Nombre,
    Guid CategoriaId,
    string? Descripcion,
    string Email,
    string Telefono,
    string Direccion,
    string CodigoPostal,
    string Ciudad,
    int IntervaloHuecosMinutos,
    int AntelacionMinimaMinutos,
    int AntelacionMaximaDias);
