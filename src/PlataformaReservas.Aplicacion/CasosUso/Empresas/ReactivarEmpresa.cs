using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;

namespace PlataformaReservas.Aplicacion.CasosUso.Empresas;

public sealed class ReactivarEmpresa(
    IRepositorioEmpresas empresas,
    IContextoEmpresa contexto,
    IUnidadTrabajo unidadTrabajo,
    IRelojSistema reloj)
{
    public async Task<Resultado> EjecutarAsync(CancellationToken ct)
    {
        Empresa? empresa = await empresas.ObtenerParaModificarAsync(contexto.EmpresaActualId, ct);
        if (empresa is null)
        {
            return Resultado.Fallo(CodigoError.NoEncontrado, "No se encuentra la empresa.");
        }

        if (empresa.Activa)
        {
            return Resultado.Fallo(CodigoError.EstadoNoValido, "La empresa ya esta activa.");
        }

        empresa.Reactivar(reloj.AhoraUtc);
        await unidadTrabajo.GuardarCambiosAsync(ct);
        return Resultado.Exito();
    }
}
