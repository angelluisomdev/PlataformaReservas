using FluentAssertions;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Notificaciones;
using PlataformaReservas.Dominio.Eventos;
using PlataformaReservas.PruebasUnitarias.Infraestructura;

namespace PlataformaReservas.PruebasUnitarias.Notificaciones;

public sealed class PruebasEnviarCorreoCuentaDadaDeBaja
{
    private static readonly DateTime Ahora = new(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Envia_la_confirmacion_al_usuario_con_el_recuento_del_instante_del_evento()
    {
        CorreoCaptura correo = new();
        ConsultasNotificacionFalsas consultas = new() { Cuenta = ConsultasNotificacionFalsas.UnaCuenta() };

        await new EnviarCorreoCuentaDadaDeBaja(consultas, new RenderizadorFalso(), correo)
            .ManejarAsync(new CuentaDadaDeBaja(Guid.CreateVersion7(), Ahora), CancellationToken.None);

        correo.Enviados.Should().Equal(new MensajeCorreo("lucia@ejemplo.es", nameof(IRenderizadorPlantillas.CuentaDadaDeBajaAsync), ""));
        consultas.InstantesPedidos.Should().Equal(Ahora);
    }

    [Fact]
    public async Task Si_el_usuario_no_existe_no_envia_nada()
    {
        CorreoCaptura correo = new();

        await new EnviarCorreoCuentaDadaDeBaja(new ConsultasNotificacionFalsas(), new RenderizadorFalso(), correo)
            .ManejarAsync(new CuentaDadaDeBaja(Guid.CreateVersion7(), Ahora), CancellationToken.None);

        correo.Enviados.Should().BeEmpty();
    }
}
