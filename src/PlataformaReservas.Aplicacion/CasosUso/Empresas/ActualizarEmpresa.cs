using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.CasosUso.Empresas;

public sealed class ActualizarEmpresa(
    IRepositorioEmpresas empresas,
    IConsultasCatalogoPublico catalogo,
    IContextoEmpresa contexto,
    IUnidadTrabajo unidadTrabajo,
    IRelojSistema reloj)
{
    public async Task<Resultado> EjecutarAsync(ActualizarEmpresaComando comando, CancellationToken ct)
    {
        DatosEmpresa datos;
        PoliticasReserva politicas;
        try
        {
            datos = DatosEmpresa.Desde(
                comando.Nombre, comando.Descripcion, comando.Email, comando.Telefono,
                comando.Direccion, comando.CodigoPostal, comando.Ciudad);
            politicas = new PoliticasReserva(
                comando.IntervaloHuecosMinutos, comando.AntelacionMinimaMinutos, comando.AntelacionMaximaDias);
        }
        catch (DominioException error)
        {
            return Resultado.Fallo(CodigoError.Validacion, error.Message);
        }

        IReadOnlyList<CategoriaDto> categorias = await catalogo.ObtenerCategoriasAsync(ct);
        if (!categorias.Any(c => c.Id == comando.CategoriaId))
        {
            return Resultado.Fallo(CodigoError.Validacion, "La categoria elegida no existe.");
        }

        Empresa? empresa = await empresas.ObtenerParaModificarAsync(contexto.EmpresaActualId, ct);
        if (empresa is null)
        {
            return Resultado.Fallo(CodigoError.NoEncontrado, "No se encuentra la empresa.");
        }

        DateTime ahora = reloj.AhoraUtc;
        empresa.ActualizarDatos(
            datos.Nombre, datos.Descripcion, datos.Email, datos.Telefono, datos.Direccion, datos.CodigoPostal, datos.Ciudad, ahora);
        empresa.CambiarCategoria(comando.CategoriaId, ahora);
        empresa.CambiarPoliticas(politicas, ahora);

        await unidadTrabajo.GuardarCambiosAsync(ct);
        return Resultado.Exito();
    }
}
