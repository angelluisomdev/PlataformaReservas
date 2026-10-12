using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos.Empresas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;

namespace PlataformaReservas.Aplicacion.CasosUso.Empresas;

public sealed class ObtenerConfiguracionEmpresa(
    IRepositorioEmpresas empresas,
    IRepositorioReservas reservas,
    IContextoEmpresa contexto,
    IRelojSistema reloj)
{
    public async Task<Resultado<ConfiguracionEmpresaDto>> EjecutarAsync(CancellationToken ct)
    {
        Empresa? empresa = await empresas.ObtenerPorIdAsync(contexto.EmpresaActualId, ct);
        if (empresa is null)
        {
            return Resultado<ConfiguracionEmpresaDto>.Fallo(CodigoError.NoEncontrado, "No se encuentra la empresa.");
        }

        int reservasFuturas = await reservas.ContarFuturasConfirmadasAsync(reloj.AhoraUtc, ct);

        return Resultado<ConfiguracionEmpresaDto>.Exito(ConfiguracionEmpresaDto.ConvertirADto(empresa, reservasFuturas));
    }
}
