using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.Repositorios;

namespace PlataformaReservas.Aplicacion.CasosUso.Reservas;

public sealed class CancelarReservaEmpresa(IRepositorioReservas reservas, IUnidadTrabajo unidadTrabajo, IRelojSistema reloj)
{
    public async Task<Resultado> EjecutarAsync(CancelarReservaEmpresaComando comando, CancellationToken ct)
    {
        Reserva? reserva = await reservas.ObtenerParaModificarAsync(comando.ReservaId, ct);
        if (reserva is null)
        {
            return Resultado.Fallo(CodigoError.NoEncontrado, "No se encuentra la reserva.");
        }

        if (reserva.Estado != EstadoReserva.Confirmada)
        {
            return Resultado.Fallo(CodigoError.EstadoNoValido, "Solo se cancelan reservas confirmadas.");
        }

        reserva.CancelarPorEmpresa(reloj.AhoraUtc);
        await unidadTrabajo.GuardarCambiosAsync(ct);
        return Resultado.Exito();
    }
}
