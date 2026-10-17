using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Disponibilidad;

namespace PlataformaReservas.Aplicacion.CasosUso.Reservas;

public sealed class ConsultarDisponibilidad(IMotorDisponibilidad motor)
{
    public async Task<Resultado<IReadOnlyList<HuecoDisponible>>> EjecutarAsync(ConsultarDisponibilidadComando comando, CancellationToken ct)
    {
        IReadOnlyList<HuecoDisponible> huecos = await motor.ObtenerHuecosAsync(
            comando.EmpresaId, comando.ServicioId, comando.ProfesionalId, comando.Fecha, ct);
        return Resultado<IReadOnlyList<HuecoDisponible>>.Exito(huecos);
    }
}
