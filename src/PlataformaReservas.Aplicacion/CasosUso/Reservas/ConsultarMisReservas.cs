using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos.Reservas;

namespace PlataformaReservas.Aplicacion.CasosUso.Reservas;

public sealed class ConsultarMisReservas(IContextoUsuario contexto, IConsultasReservasCliente consultas, IRelojSistema reloj)
{
    public async Task<Resultado<MisReservasDto>> EjecutarAsync(CancellationToken ct)
    {
        if (!contexto.TieneUsuario)
        {
            return Resultado<MisReservasDto>.Fallo(CodigoError.NoAutorizado, "Hay que iniciar sesion para ver tus reservas.");
        }

        return Resultado<MisReservasDto>.Exito(await consultas.ObtenerMisReservasAsync(reloj.AhoraUtc, ct));
    }
}
