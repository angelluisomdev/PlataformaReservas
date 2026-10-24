using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Notificaciones;

namespace PlataformaReservas.PruebasUnitarias.Infraestructura;

public sealed class ConsultasNotificacionFalsas : IConsultasNotificacion
{
    public DatosReservaNotificacionDto? Reserva { get; init; }

    public DatosEmpresaNotificacionDto? Empresa { get; init; }

    public DatosCuentaNotificacionDto? Cuenta { get; init; }

    public List<DateTime> InstantesPedidos { get; } = [];

    public static DatosReservaNotificacionDto UnaReserva()
    {
        return new DatosReservaNotificacionDto(
            Guid.CreateVersion7(), "Barberia Centro", "Calle Mayor 1, 37001 Salamanca", "contacto@barberia.es", "Corte", "Ana Garcia",
            new DateTime(2026, 10, 5, 7, 30, 0, DateTimeKind.Utc), 45, 18.50m, "Lucia Fernandez", "611222333", "lucia@ejemplo.es");
    }

    public static DatosEmpresaNotificacionDto UnaEmpresa()
    {
        return new DatosEmpresaNotificacionDto(
            Guid.CreateVersion7(), "Barberia Centro", "barberia-centro", "ana@barberia.es", "Ana Garcia", true, true, true, true, 2);
    }

    public static DatosCuentaNotificacionDto UnaCuenta()
    {
        return new DatosCuentaNotificacionDto(Guid.CreateVersion7(), "lucia@ejemplo.es", "Lucia Fernandez", 1);
    }

    public Task<DatosReservaNotificacionDto?> ObtenerDatosReservaAsync(Guid reservaId, CancellationToken ct)
    {
        return Task.FromResult(Reserva);
    }

    public Task<DatosEmpresaNotificacionDto?> ObtenerDatosEmpresaAsync(Guid empresaId, DateTime instanteEvento, CancellationToken ct)
    {
        InstantesPedidos.Add(instanteEvento);
        return Task.FromResult(Empresa);
    }

    public Task<DatosCuentaNotificacionDto?> ObtenerDatosCuentaAsync(Guid usuarioId, DateTime instanteEvento, CancellationToken ct)
    {
        InstantesPedidos.Add(instanteEvento);
        return Task.FromResult(Cuenta);
    }
}
