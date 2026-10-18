using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos.Reservas;

namespace PlataformaReservas.Aplicacion.CasosUso.Reservas;

public sealed class ConsultarDetalleReserva(IContextoUsuario contexto, IConsultasReservasCliente consultas, IRelojSistema reloj)
{
    public async Task<Resultado<DetalleReservaClienteDto>> EjecutarAsync(ConsultarDetalleReservaComando comando, CancellationToken ct)
    {
        if (!contexto.TieneUsuario)
        {
            return Resultado<DetalleReservaClienteDto>.Fallo(CodigoError.NoAutorizado, "Hay que iniciar sesion para ver la reserva.");
        }

        DetalleReservaClienteDto? detalle = await consultas.ObtenerDetalleAsync(comando.ReservaId, reloj.AhoraUtc, ct);
        return detalle is null
            ? Resultado<DetalleReservaClienteDto>.Fallo(CodigoError.NoEncontrado, "No se encuentra la reserva.")
            : Resultado<DetalleReservaClienteDto>.Exito(detalle);
    }
}
