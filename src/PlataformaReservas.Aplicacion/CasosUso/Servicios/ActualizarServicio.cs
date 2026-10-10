using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;

namespace PlataformaReservas.Aplicacion.CasosUso.Servicios;

public sealed class ActualizarServicio(IRepositorioServicios servicios, IUnidadTrabajo unidadTrabajo, IRelojSistema reloj)
{
    public async Task<Resultado> EjecutarAsync(ActualizarServicioComando comando, CancellationToken ct)
    {
        DatosServicio datos;
        try
        {
            datos = DatosServicio.Desde(
                comando.Nombre, comando.Descripcion, comando.DuracionMinutos, comando.Precio,
                comando.IntervaloHuecosMinutos, comando.AntelacionMinimaMinutos, comando.AntelacionMaximaDias);
        }
        catch (DominioException error)
        {
            return Resultado.Fallo(CodigoError.Validacion, error.Message);
        }

        Servicio? servicio = await servicios.ObtenerParaModificarAsync(comando.Id, ct);
        if (servicio is null)
        {
            return Resultado.Fallo(CodigoError.NoEncontrado, "No se encuentra el servicio.");
        }

        DateTime ahora = reloj.AhoraUtc;
        servicio.ActualizarDatos(datos.Nombre, datos.Descripcion, datos.Duracion, datos.Precio, ahora);
        servicio.EstablecerPoliticas(datos.IntervaloHuecos, datos.AntelacionMinima, datos.AntelacionMaxima, ahora);

        await unidadTrabajo.GuardarCambiosAsync(ct);
        return Resultado.Exito();
    }
}
