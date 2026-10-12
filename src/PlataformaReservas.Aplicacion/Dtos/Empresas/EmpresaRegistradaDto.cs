using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Aplicacion.Dtos.Empresas;

public sealed record EmpresaRegistradaDto(Guid UsuarioId, Guid EmpresaId, string Slug)
{
    public static EmpresaRegistradaDto ConvertirADto(Empresa empresa, Guid usuarioId)
    {
        return new EmpresaRegistradaDto(usuarioId, empresa.Id, empresa.Slug.Valor);
    }
}
