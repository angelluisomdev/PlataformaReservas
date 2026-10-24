using FluentAssertions;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Notificaciones;
using PlataformaReservas.Dominio.Eventos;
using PlataformaReservas.PruebasUnitarias.Infraestructura;

namespace PlataformaReservas.PruebasUnitarias.Notificaciones;

public sealed class PruebasEnviarCorreosReservaConfirmada
{
    private static readonly DateTime Ahora = new(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Envia_la_confirmacion_al_cliente_y_el_aviso_al_negocio()
    {
        CorreoCaptura correo = new();
        ConsultasNotificacionFalsas consultas = new() { Reserva = ConsultasNotificacionFalsas.UnaReserva() };

        await new EnviarCorreosReservaConfirmada(consultas, new RenderizadorFalso(), correo)
            .ManejarAsync(new ReservaConfirmada(Guid.CreateVersion7(), Ahora), CancellationToken.None);

        correo.Enviados.Should().Equal(
            new MensajeCorreo("lucia@ejemplo.es", nameof(IRenderizadorPlantillas.ReservaConfirmadaClienteAsync), ""),
            new MensajeCorreo("contacto@barberia.es", nameof(IRenderizadorPlantillas.ReservaConfirmadaNegocioAsync), ""));
    }

    [Fact]
    public async Task Si_la_reserva_ya_no_existe_no_envia_nada()
    {
        CorreoCaptura correo = new();

        await new EnviarCorreosReservaConfirmada(new ConsultasNotificacionFalsas(), new RenderizadorFalso(), correo)
            .ManejarAsync(new ReservaConfirmada(Guid.CreateVersion7(), Ahora), CancellationToken.None);

        correo.Enviados.Should().BeEmpty();
    }
}
