using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.Repositorios;

namespace PlataformaReservas.Aplicacion.CasosUso.Reservas;

public sealed class CompletarReserva(IRepositorioReservas reservas, IUnidadTrabajo unidadTrabajo, IRelojSistema reloj)
{
    public async Task<Resultado> EjecutarAsync(CompletarReservaComando comando, CancellationToken ct)
    {
        Reserva? reserva = await reservas.ObtenerParaModificarAsync(comando.ReservaId, ct);
        if (reserva is null)
        {
            return Resultado.Fallo(CodigoError.NoEncontrado, "No se encuentra la reserva.");
        }

        DateTime ahora = reloj.AhoraUtc;
        if (reserva.Estado != EstadoReserva.Confirmada)
        {
            return Resultado.Fallo(CodigoError.EstadoNoValido, "Solo se completan reservas confirmadas.");
        }

        if (reserva.Franja.FinUtc >= ahora)
        {
            return Resultado.Fallo(CodigoError.EstadoNoValido, "La reserva aun no ha terminado.");
        }

        reserva.Completar(ahora);
        await unidadTrabajo.GuardarCambiosAsync(ct);
        return Resultado.Exito();
    }
}
