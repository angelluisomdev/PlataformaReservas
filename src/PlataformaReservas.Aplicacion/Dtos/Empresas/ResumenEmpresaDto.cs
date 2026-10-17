using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Aplicacion.Dtos.Empresas;

public sealed record ResumenEmpresaDto(string Nombre, string Slug, string Categoria, string Ciudad, string? DescripcionBreve)
{
    public const int LongitudDescripcionBreve = 160;

    public static ResumenEmpresaDto ConvertirADto(Empresa empresa, string categoria)
    {
        string? descripcion = empresa.Descripcion?.Valor;
        string? breve = descripcion is { Length: > LongitudDescripcionBreve }
            ? descripcion[..LongitudDescripcionBreve]
            : descripcion;
        return new ResumenEmpresaDto(empresa.Nombre.Valor, empresa.Slug.Valor, categoria, empresa.Ciudad.Valor, breve);
    }
}
