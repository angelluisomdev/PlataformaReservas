namespace PlataformaReservas.Aplicacion.CasosUso.Empresas;

public sealed record RegistrarEmpresaComando(
    string NombreCompleto,
    string Email,
    string? Telefono,
    string Clave,
    string NombreEmpresa,
    Guid CategoriaId,
    string? Descripcion,
    string Direccion,
    string Ciudad,
    string CodigoPostal,
    string TelefonoEmpresa,
    string EmailEmpresa);
