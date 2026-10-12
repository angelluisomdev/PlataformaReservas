using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.Repositorios;

namespace PlataformaReservas.Aplicacion.CasosUso.Reservas;

public sealed class CancelarReservaCliente(
    IContextoUsuario contexto,
    IRepositorioReservasCliente reservas,
    IUnidadTrabajo unidadTrabajo,
    IPublicadorEventos publicador,
    IRelojSistema reloj)
{
    public async Task<Resultado> EjecutarAsync(CancelarReservaClienteComando comando, CancellationToken ct)
    {
        if (!contexto.TieneUsuario)
        {
            return Resultado.Fallo(CodigoError.NoAutorizado, "Hay que iniciar sesion para cancelar una reserva.");
        }

        Reserva? reserva = await reservas.ObtenerParaModificarAsync(comando.ReservaId, ct);
        if (reserva is null)
        {
            return Resultado.Fallo(CodigoError.NoEncontrado, "No se encuentra la reserva.");
        }

        DateTime ahora = reloj.AhoraUtc;
        if (reserva.Estado != EstadoReserva.Confirmada)
        {
            return Resultado.Fallo(CodigoError.EstadoNoValido, "Solo se cancelan reservas confirmadas.");
        }

        if (reserva.Franja.InicioUtc <= ahora)
        {
            return Resultado.Fallo(CodigoError.EstadoNoValido, "La reserva ya ha comenzado y no se puede cancelar.");
        }

        reserva.CancelarPorCliente(ahora);
        await unidadTrabajo.GuardarCambiosAsync(ct);

        await publicador.PublicarAsync(reserva.Eventos, ct);
        reserva.LimpiarEventos();
        return Resultado.Exito();
    }
}
