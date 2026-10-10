using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;

namespace PlataformaReservas.Aplicacion.CasosUso.Servicios;

public sealed class CrearServicio(
    IRepositorioServicios servicios,
    IContextoEmpresa contexto,
    IUnidadTrabajo unidadTrabajo,
    IRelojSistema reloj)
{
    public async Task<Resultado<Guid>> EjecutarAsync(CrearServicioComando comando, CancellationToken ct)
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
            return Resultado<Guid>.Fallo(CodigoError.Validacion, error.Message);
        }

        DateTime ahora = reloj.AhoraUtc;
        Servicio servicio = Servicio.Crear(
            Guid.CreateVersion7(), contexto.EmpresaActualId, datos.Nombre, datos.Duracion, datos.Precio, ahora);
        servicio.ActualizarDatos(datos.Nombre, datos.Descripcion, datos.Duracion, datos.Precio, ahora);
        servicio.EstablecerPoliticas(datos.IntervaloHuecos, datos.AntelacionMinima, datos.AntelacionMaxima, ahora);
        servicios.Agregar(servicio);

        await unidadTrabajo.GuardarCambiosAsync(ct);
        return Resultado<Guid>.Exito(servicio.Id);
    }
}
