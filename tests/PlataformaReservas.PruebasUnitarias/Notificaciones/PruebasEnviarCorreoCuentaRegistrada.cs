using FluentAssertions;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Notificaciones;
using PlataformaReservas.Dominio.Eventos;
using PlataformaReservas.PruebasUnitarias.Infraestructura;

namespace PlataformaReservas.PruebasUnitarias.Notificaciones;

public sealed class PruebasEnviarCorreoCuentaRegistrada
{
    private static readonly DateTime Ahora = new(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Envia_la_bienvenida_al_usuario()
    {
        CorreoCaptura correo = new();
        ConsultasNotificacionFalsas consultas = new() { Cuenta = ConsultasNotificacionFalsas.UnaCuenta() };

        await new EnviarCorreoCuentaRegistrada(consultas, new RenderizadorFalso(), correo)
            .ManejarAsync(new CuentaRegistrada(Guid.CreateVersion7(), Ahora), CancellationToken.None);

        correo.Enviados.Should().Equal(new MensajeCorreo("lucia@ejemplo.es", nameof(IRenderizadorPlantillas.CuentaRegistradaAsync), ""));
    }

    [Fact]
    public async Task Si_el_usuario_no_existe_no_envia_nada()
    {
        CorreoCaptura correo = new();

        await new EnviarCorreoCuentaRegistrada(new ConsultasNotificacionFalsas(), new RenderizadorFalso(), correo)
            .ManejarAsync(new CuentaRegistrada(Guid.CreateVersion7(), Ahora), CancellationToken.None);

        correo.Enviados.Should().BeEmpty();
    }
}
