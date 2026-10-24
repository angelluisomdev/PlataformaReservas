using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Notificaciones;
using PlataformaReservas.Infraestructura.Correo;

namespace PlataformaReservas.PruebasIntegracion.Correo;

public sealed class PruebasRenderizadorPlantillas : IAsyncDisposable
{
    private static readonly DateTime NueveYMediaLunesUtc = new(2026, 10, 5, 7, 30, 0, DateTimeKind.Utc);

    private readonly ServiceProvider _servicios = new ServiceCollection().AddLogging().BuildServiceProvider();
    private readonly RenderizadorPlantillas _renderizador;

    public PruebasRenderizadorPlantillas()
    {
        IConfiguration configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Correo:UrlBase"] = "https://reservas.example/" })
            .Build();
        _renderizador = new RenderizadorPlantillas(_servicios, NullLoggerFactory.Instance, configuracion);
    }

    [Fact]
    public async Task Cada_correo_va_envuelto_en_el_layout_comun()
    {
        MensajeCorreo[] mensajes =
        [
            await _renderizador.ReservaConfirmadaClienteAsync(Datos()),
            await _renderizador.ReservaConfirmadaNegocioAsync(Datos()),
            await _renderizador.ReservaCanceladaPorClienteAsync(Datos()),
            await _renderizador.ReservaCanceladaPorEmpresaAsync(Datos()),
            await _renderizador.ReservaCanceladaPorBajaEmpresaAsync(Datos()),
            await _renderizador.ReservaCanceladaPorBajaCuentaAsync(Datos()),
            await _renderizador.EmpresaRegistradaAsync(Empresa()),
            await _renderizador.EmpresaDadaDeBajaAsync(Empresa()),
            await _renderizador.CuentaRegistradaAsync(Cuenta()),
            await _renderizador.CuentaDadaDeBajaAsync(Cuenta()),
            await _renderizador.RestablecimientoClaveAsync("lucia@ejemplo.es", "Lucia Fernandez", "https://reservas.example/x"),
        ];

        mensajes.Should().HaveCount(11).And.AllSatisfy(m =>
        {
            m.CuerpoHtml.Should().StartWith("<!DOCTYPE html>").And.Contain("data-pie").And.Contain("Plataforma de Reservas");
            m.Asunto.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public async Task Reserva_confirmada_al_cliente_lleva_establecimiento_direccion_servicio_profesional_fecha_hora_duracion_y_precio()
    {
        MensajeCorreo mensaje = await _renderizador.ReservaConfirmadaClienteAsync(Datos());

        mensaje.Para.Should().Be("lucia@ejemplo.es");
        mensaje.Asunto.Should().Contain("Barberia Centro");
        mensaje.CuerpoHtml.Should()
            .Contain("Barberia Centro")
            .And.Contain("Calle Mayor 1, 37001 Salamanca")
            .And.Contain("Corte y barba")
            .And.Contain("Ana Garcia")
            .And.Contain("lunes 5 de octubre de 2026")
            .And.Contain("09:30")
            .And.Contain("45 minutos")
            .And.Contain("18,50");
    }

    [Fact]
    public async Task Reserva_confirmada_al_negocio_lleva_servicio_profesional_fecha_hora_y_nombre_y_telefono_del_cliente()
    {
        MensajeCorreo mensaje = await _renderizador.ReservaConfirmadaNegocioAsync(Datos());

        mensaje.Para.Should().Be("contacto@barberia.es");
        mensaje.CuerpoHtml.Should()
            .Contain("Corte y barba")
            .And.Contain("Ana Garcia")
            .And.Contain("lunes 5 de octubre de 2026")
            .And.Contain("09:30")
            .And.Contain("Lucia Fernandez")
            .And.Contain("611222333");
    }

    [Fact]
    public async Task Reserva_cancelada_por_el_cliente_va_al_negocio_y_dice_que_la_cancela_el_cliente()
    {
        MensajeCorreo mensaje = await _renderizador.ReservaCanceladaPorClienteAsync(Datos());

        mensaje.Para.Should().Be("contacto@barberia.es");
        mensaje.Asunto.Should().Contain("cancelada por el cliente");
        mensaje.CuerpoHtml.Should()
            .Contain("El cliente ha cancelado")
            .And.Contain("Corte y barba")
            .And.Contain("09:30")
            .And.Contain("Lucia Fernandez")
            .And.Contain("611222333");
    }

    [Fact]
    public async Task Reserva_cancelada_por_la_empresa_va_al_cliente_y_dice_que_la_cancela_el_establecimiento()
    {
        MensajeCorreo mensaje = await _renderizador.ReservaCanceladaPorEmpresaAsync(Datos());

        mensaje.Para.Should().Be("lucia@ejemplo.es");
        mensaje.Asunto.Should().Contain("Barberia Centro ha cancelado tu reserva");
        mensaje.CuerpoHtml.Should()
            .Contain("ha cancelado tu reserva")
            .And.Contain("Corte y barba")
            .And.Contain("Ana Garcia")
            .And.Contain("lunes 5 de octubre de 2026")
            .And.Contain("09:30");
    }

    [Fact]
    public async Task Reserva_cancelada_por_baja_de_la_empresa_va_al_cliente_y_dice_que_el_establecimiento_ha_cerrado()
    {
        MensajeCorreo mensaje = await _renderizador.ReservaCanceladaPorBajaEmpresaAsync(Datos());

        mensaje.Para.Should().Be("lucia@ejemplo.es");
        mensaje.CuerpoHtml.Should()
            .Contain("ha cerrado")
            .And.Contain("queda anulada")
            .And.Contain("Corte y barba")
            .And.Contain("lunes 5 de octubre de 2026")
            .And.Contain("09:30");
    }

    [Fact]
    public async Task Reserva_cancelada_por_baja_de_la_cuenta_va_al_negocio_y_dice_que_el_cliente_se_ha_dado_de_baja()
    {
        MensajeCorreo mensaje = await _renderizador.ReservaCanceladaPorBajaCuentaAsync(Datos());

        mensaje.Para.Should().Be("contacto@barberia.es");
        mensaje.CuerpoHtml.Should()
            .Contain("ha dado de baja su cuenta")
            .And.Contain("Corte y barba")
            .And.Contain("09:30")
            .And.Contain("Lucia Fernandez");
    }

    [Fact]
    public async Task Empresa_registrada_va_al_propietario_con_el_enlace_a_la_ficha_y_los_pasos_que_faltan()
    {
        DatosEmpresaNotificacionDto sinProfesional = Empresa() with
        {
            TieneProfesionalActivo = false,
            TieneAsociacion = false,
            EsPublicable = false,
        };

        MensajeCorreo mensaje = await _renderizador.EmpresaRegistradaAsync(sinProfesional);

        mensaje.Para.Should().Be("ana@barberia.es");
        mensaje.CuerpoHtml.Should()
            .Contain("https://reservas.example/businesses/barberia-centro")
            .And.Contain("Crear un profesional")
            .And.Contain("Asociar un profesional a un servicio")
            .And.Contain("Definir el horario")
            .And.NotContain("Crear un servicio");
    }

    [Fact]
    public async Task Empresa_registrada_y_ya_publicable_no_lista_pasos()
    {
        MensajeCorreo mensaje = await _renderizador.EmpresaRegistradaAsync(Empresa());

        mensaje.CuerpoHtml.Should().Contain("Ya es visible").And.NotContain("Definir el horario");
    }

    [Fact]
    public async Task Empresa_dada_de_baja_va_al_propietario_con_el_recuento_y_como_reactivar()
    {
        MensajeCorreo mensaje = await _renderizador.EmpresaDadaDeBajaAsync(Empresa() with { ReservasCanceladas = 7 });

        mensaje.Para.Should().Be("ana@barberia.es");
        mensaje.CuerpoHtml.Should()
            .Contain("Se han cancelado 7 reserva(s)")
            .And.Contain("https://reservas.example/app/settings");
    }

    [Fact]
    public async Task Cuenta_registrada_da_la_bienvenida_al_usuario()
    {
        MensajeCorreo mensaje = await _renderizador.CuentaRegistradaAsync(Cuenta());

        mensaje.Para.Should().Be("lucia@ejemplo.es");
        mensaje.Asunto.Should().Contain("bienvenida");
        mensaje.CuerpoHtml.Should().Contain("Lucia Fernandez");
    }

    [Fact]
    public async Task Cuenta_dada_de_baja_confirma_con_el_recuento()
    {
        MensajeCorreo mensaje = await _renderizador.CuentaDadaDeBajaAsync(Cuenta() with { ReservasCanceladas = 3 });

        mensaje.Para.Should().Be("lucia@ejemplo.es");
        mensaje.CuerpoHtml.Should().Contain("Se han cancelado 3 reserva(s)");
    }

    [Fact]
    public async Task Restablecimiento_lleva_el_enlace_con_el_token_codificado_una_sola_vez()
    {
        const string enlace = "https://reservas.example/Account/ResetPassword?code=AbC-123_x&otro=1";

        MensajeCorreo mensaje = await _renderizador.RestablecimientoClaveAsync("lucia@ejemplo.es", "Lucia Fernandez", enlace);

        mensaje.Para.Should().Be("lucia@ejemplo.es");
        mensaje.CuerpoHtml.Should()
            .Contain("href=\"https://reservas.example/Account/ResetPassword?code=AbC-123_x&amp;otro=1\"")
            .And.NotContain("&amp;amp;");
    }

    [Fact]
    public async Task La_hora_es_la_local_de_madrid_tambien_en_invierno()
    {
        DatosReservaNotificacionDto enero = Datos() with { InicioUtc = new DateTime(2026, 1, 12, 23, 30, 0, DateTimeKind.Utc) };

        MensajeCorreo mensaje = await _renderizador.ReservaConfirmadaClienteAsync(enero);

        mensaje.CuerpoHtml.Should().Contain("martes 13 de enero de 2026").And.Contain("00:30").And.NotContain("23:30");
    }

    [Fact]
    public async Task Un_script_en_el_nombre_del_establecimiento_sale_escapado_en_el_html()
    {
        DatosReservaNotificacionDto malicioso = Datos() with
        {
            Establecimiento = "<script>alert(1)</script>",
            NombreCliente = "<img src=x onerror=alert(2)>",
        };

        MensajeCorreo[] mensajes =
        [
            await _renderizador.ReservaConfirmadaClienteAsync(malicioso),
            await _renderizador.ReservaConfirmadaNegocioAsync(malicioso),
            await _renderizador.ReservaCanceladaPorClienteAsync(malicioso),
            await _renderizador.ReservaCanceladaPorEmpresaAsync(malicioso),
        ];

        DatosEmpresaNotificacionDto empresaMaliciosa = Empresa() with { Nombre = "<script>alert(3)</script>" };
        mensajes =
        [
            .. mensajes,
            await _renderizador.ReservaCanceladaPorBajaEmpresaAsync(malicioso),
            await _renderizador.ReservaCanceladaPorBajaCuentaAsync(malicioso),
            await _renderizador.EmpresaRegistradaAsync(empresaMaliciosa),
            await _renderizador.EmpresaDadaDeBajaAsync(empresaMaliciosa),
        ];

        mensajes.Should().AllSatisfy(m =>
        {
            m.CuerpoHtml.Should().NotContain("<script>").And.NotContain("<img");
            m.CuerpoHtml.Should().Contain("&lt;");
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _renderizador.DisposeAsync();
        await _servicios.DisposeAsync();
    }

    private static DatosEmpresaNotificacionDto Empresa()
    {
        return new DatosEmpresaNotificacionDto(
            Guid.CreateVersion7(), "Barberia Centro", "barberia-centro", "ana@barberia.es", "Ana Garcia", true, true, true, true, 0);
    }

    private static DatosCuentaNotificacionDto Cuenta()
    {
        return new DatosCuentaNotificacionDto(Guid.CreateVersion7(), "lucia@ejemplo.es", "Lucia Fernandez", 0);
    }

    private static DatosReservaNotificacionDto Datos()
    {
        return new DatosReservaNotificacionDto(
            Guid.CreateVersion7(), "Barberia Centro", "Calle Mayor 1, 37001 Salamanca", "contacto@barberia.es", "Corte y barba",
            "Ana Garcia", NueveYMediaLunesUtc, 45, 18.50m, "Lucia Fernandez", "611222333", "lucia@ejemplo.es");
    }
}
