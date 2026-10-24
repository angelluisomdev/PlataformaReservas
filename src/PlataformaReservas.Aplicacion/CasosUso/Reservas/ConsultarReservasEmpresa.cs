using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Disponibilidad;
using PlataformaReservas.Aplicacion.Dtos.Reservas;

namespace PlataformaReservas.Aplicacion.CasosUso.Reservas;

public sealed class ConsultarReservasEmpresa(IConsultasReservasEmpresa consultas, IRelojSistema reloj)
{
    public async Task<Resultado<IReadOnlyList<ReservaEmpresaDto>>> EjecutarAsync(
        ConsultarReservasEmpresaComando comando, CancellationToken ct)
    {
        if (comando.Estado is { } estado && !Enum.IsDefined(estado))
        {
            return Resultado<IReadOnlyList<ReservaEmpresaDto>>.Fallo(CodigoError.Validacion, "El estado no es valido.");
        }

        if (comando.Fecha == DateOnly.MinValue || comando.Fecha == DateOnly.MaxValue)
        {
            return Resultado<IReadOnlyList<ReservaEmpresaDto>>.Fallo(CodigoError.Validacion, "La fecha no es valida.");
        }

        (DateTime desde, DateTime hasta) = MotorDisponibilidad.DiaLocalEnUtc(comando.Fecha);
        IReadOnlyList<ReservaEmpresaDto> reservas = await consultas.ObtenerReservasAsync(
            desde, hasta, comando.ProfesionalId, comando.Estado, reloj.AhoraUtc, ct);
        return Resultado<IReadOnlyList<ReservaEmpresaDto>>.Exito(reservas);
    }
}
