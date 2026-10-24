using FluentAssertions;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Notificaciones;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.Eventos;
using PlataformaReservas.PruebasUnitarias.Infraestructura;

namespace PlataformaReservas.PruebasUnitarias.Notificaciones;

public sealed class PruebasEnviarCorreoReservaCancelada
{
    private static readonly DateTime Ahora = new(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(OrigenCancelacion.Cliente, "contacto@barberia.es", nameof(IRenderizadorPlantillas.ReservaCanceladaPorClienteAsync))]
    [InlineData(OrigenCancelacion.Empresa, "lucia@ejemplo.es", nameof(IRenderizadorPlantillas.ReservaCanceladaPorEmpresaAsync))]
    [InlineData(OrigenCancelacion.BajaEmpresa, "lucia@ejemplo.es", nameof(IRenderizadorPlantillas.ReservaCanceladaPorBajaEmpresaAsync))]
    [InlineData(OrigenCancelacion.BajaCuenta, "contacto@barberia.es", nameof(IRenderizadorPlantillas.ReservaCanceladaPorBajaCuentaAsync))]
    public async Task Elige_destinatario_y_plantilla_segun_el_origen(OrigenCancelacion origen, string destinatario, string plantilla)
    {
        CorreoCaptura correo = new();
        ConsultasNotificacionFalsas consultas = new() { Reserva = ConsultasNotificacionFalsas.UnaReserva() };

        await new EnviarCorreoReservaCancelada(consultas, new RenderizadorFalso(), correo)
            .ManejarAsync(new ReservaCancelada(Guid.CreateVersion7(), origen, Ahora), CancellationToken.None);

        correo.Enviados.Should().Equal(new MensajeCorreo(destinatario, plantilla, ""));
    }

    [Fact]
    public async Task Si_la_reserva_ya_no_existe_no_envia_nada()
    {
        CorreoCaptura correo = new();

        await new EnviarCorreoReservaCancelada(new ConsultasNotificacionFalsas(), new RenderizadorFalso(), correo)
            .ManejarAsync(new ReservaCancelada(Guid.CreateVersion7(), OrigenCancelacion.Cliente, Ahora), CancellationToken.None);

        correo.Enviados.Should().BeEmpty();
    }

    [Fact]
    public async Task Un_origen_desconocido_lanza_y_no_envia_nada()
    {
        CorreoCaptura correo = new();
        ConsultasNotificacionFalsas consultas = new() { Reserva = ConsultasNotificacionFalsas.UnaReserva() };

        Func<Task> manejar = () => new EnviarCorreoReservaCancelada(consultas, new RenderizadorFalso(), correo)
            .ManejarAsync(new ReservaCancelada(Guid.CreateVersion7(), (OrigenCancelacion)99, Ahora), CancellationToken.None);

        await manejar.Should().ThrowAsync<ArgumentOutOfRangeException>();
        correo.Enviados.Should().BeEmpty();
    }
}
