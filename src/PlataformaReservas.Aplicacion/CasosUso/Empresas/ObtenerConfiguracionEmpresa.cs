using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos;
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

        return Resultado<ConfiguracionEmpresaDto>.Exito(new ConfiguracionEmpresaDto(
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
            reservasFuturas));
    }
}
