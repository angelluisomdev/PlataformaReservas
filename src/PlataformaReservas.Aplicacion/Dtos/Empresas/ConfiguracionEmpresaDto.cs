using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Aplicacion.Dtos.Empresas;

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
    int ReservasFuturas)
{
    public static ConfiguracionEmpresaDto ConvertirADto(Empresa empresa, int reservasFuturas)
    {
        return new ConfiguracionEmpresaDto(
            empresa.Nombre.Valor,
            empresa.Slug.Valor,
            empresa.CategoriaId,
            empresa.Descripcion?.Valor,
            empresa.Email.Valor,
            empresa.Telefono.Valor,
            empresa.Direccion.Valor,
            empresa.CodigoPostal.Valor,
            empresa.Ciudad.Valor,
            empresa.Politicas.IntervaloHuecosMinutos,
            empresa.Politicas.AntelacionMinimaMinutos,
            empresa.Politicas.AntelacionMaximaDias,
            empresa.Activa,
            reservasFuturas);
    }
}
