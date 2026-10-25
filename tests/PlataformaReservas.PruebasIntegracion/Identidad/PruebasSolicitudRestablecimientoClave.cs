using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.PruebasIntegracion.Infraestructura;
using static PlataformaReservas.PruebasIntegracion.Infraestructura.UsuariosPrueba;

namespace PlataformaReservas.PruebasIntegracion.Identidad;

[Collection("BaseDatos")]
public sealed class PruebasSolicitudRestablecimientoClave(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task La_solicitud_de_restablecimiento_responde_igual_exista_o_no_la_cuenta()
    {
        string existente = CorreoUnico();
        string inexistente = CorreoUnico();
        await CrearAsync(baseDatos, existente);

        Func<Task> conCuenta = () => SolicitarRestablecimientoAsync(existente);
        Func<Task> sinCuenta = () => SolicitarRestablecimientoAsync(inexistente);

        await conCuenta.Should().NotThrowAsync();
        await sinCuenta.Should().NotThrowAsync();
        baseDatos.Remitente.EnlaceDe(existente).Should().NotBeNull();
        baseDatos.Remitente.EnlaceDe(inexistente).Should().BeNull();
    }

    [Fact]
    public async Task Cada_solicitud_registra_el_mismo_evento_sin_el_correo_exista_o_no_la_cuenta()
    {
        string existente = CorreoUnico();
        await CrearAsync(baseDatos, existente);
        RegistroCaptura<SolicitudRestablecimientoClave> conCuenta = new();
        RegistroCaptura<SolicitudRestablecimientoClave> sinCuenta = new();

        await SolicitarRegistrandoAsync(existente, conCuenta);
        await SolicitarRegistrandoAsync(CorreoUnico(), sinCuenta);

        conCuenta.Entradas.Should().Equal(sinCuenta.Entradas);
        (LogLevel nivel, string linea) = conCuenta.Entradas.Should().ContainSingle().Subject;
        nivel.Should().Be(LogLevel.Information);
        linea.Should().NotContain("@");
    }

    [Fact]
    public async Task Una_cuenta_inactiva_no_genera_enlace_de_restablecimiento()
    {
        string correo = CorreoUnico();
        Usuario usuario = await CrearAsync(baseDatos, correo);
        await DesactivarAsync(baseDatos, usuario.Id);

        await SolicitarRestablecimientoAsync(correo);

        baseDatos.Remitente.EnlaceDe(correo).Should().BeNull();
    }

    [Fact]
    public async Task Restablecer_la_contrasena_renueva_el_sello_de_seguridad()
    {
        string correo = CorreoUnico();
        Usuario creado = await CrearAsync(baseDatos, correo);
        await SolicitarRestablecimientoAsync(correo);
        string token = TokenDe(baseDatos.Remitente.EnlaceDe(correo)!);

        await using (AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope())
        {
            UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
            Usuario usuario = (await usuarios.FindByEmailAsync(correo))!;
            string selloAntes = (await usuarios.GetSecurityStampAsync(usuario))!;

            IdentityResult restablecido = await usuarios.ResetPasswordAsync(usuario, token, "ClaveNueva2!");

            restablecido.Succeeded.Should().BeTrue();
            (await usuarios.GetSecurityStampAsync(usuario)).Should().NotBe(selloAntes);
        }

        (await ComprobarClaveAsync(baseDatos, creado.Id, "ClaveNueva2!")).Succeeded.Should().BeTrue();
        (await ComprobarClaveAsync(baseDatos, creado.Id, Clave)).Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task El_enlace_se_construye_con_la_url_base_configurada_y_nunca_con_el_host_de_la_peticion()
    {
        string correo = CorreoUnico();
        await CrearAsync(baseDatos, correo);

        await SolicitarRestablecimientoAsync(correo);

        baseDatos.Remitente.EnlaceDe(correo).Should().StartWith($"{BaseDatosFixture.UrlBase}/Account/ResetPassword?code=");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/relativa")]
    [InlineData("reservas.example")]
    [InlineData("file:///etc/passwd")]
    [InlineData("javascript:alert(1)")]
    public async Task Sin_una_url_base_absoluta_no_envia_nada_y_responde_igual(string? urlBase)
    {
        string correo = CorreoUnico();
        await CrearAsync(baseDatos, correo);
        CorreoCaptura capturado = new();
        RegistroCaptura<SolicitudRestablecimientoClave> registro = new();

        Func<Task> solicitar = () => SolicitarConRemitenteRealAsync(correo, capturado, registro, urlBase);

        await solicitar.Should().NotThrowAsync();
        capturado.Enviados.Should().BeEmpty();
        registro.LineasDe(LogLevel.Error).Should().ContainSingle().Which.Should().Contain("UrlBase");
    }

    [Fact]
    public async Task Con_el_remitente_real_cliente_y_propietario_reciben_el_mismo_correo_y_un_correo_inexistente_no_recibe_nada()
    {
        string cliente = CorreoUnico();
        string propietario = CorreoUnico();
        string inexistente = CorreoUnico();
        await CrearAsync(baseDatos, cliente);
        await CrearPropietarioAsync(propietario);
        CorreoCaptura capturado = new();

        await SolicitarConRemitenteRealAsync(cliente, capturado);
        await SolicitarConRemitenteRealAsync(propietario, capturado);
        await SolicitarConRemitenteRealAsync(inexistente, capturado);

        capturado.Enviados.Select(m => m.Para).Should().Equal(cliente, propietario);
        capturado.Enviados.Should().AllSatisfy(m =>
        {
            m.Asunto.Should().Be("Restablece tu contraseña");
            m.CuerpoHtml.Should().Contain($"href=\"{BaseDatosFixture.UrlBase}/Account/ResetPassword?code=");
        });
    }

    [Fact]
    public async Task Con_el_servidor_de_correo_caido_responde_igual_que_sin_cuenta_y_no_registra_el_correo()
    {
        string existente = CorreoUnico();
        string inexistente = CorreoUnico();
        await CrearAsync(baseDatos, existente);
        CorreoCaptura caido = new() { Fallar = true };
        RegistroCaptura<SolicitudRestablecimientoClave> registro = new();

        Func<Task> conCuenta = () => SolicitarConRemitenteRealAsync(existente, caido, registro);
        Func<Task> sinCuenta = () => SolicitarConRemitenteRealAsync(inexistente, caido, registro);

        await conCuenta.Should().NotThrowAsync();
        await sinCuenta.Should().NotThrowAsync();
        string error = registro.LineasDe(LogLevel.Error).Should().ContainSingle().Subject;
        error.Should().Contain(nameof(InvalidOperationException)).And.NotContain(existente).And.NotContain("@");
    }

    [Fact]
    public async Task Un_propietario_usa_el_mismo_flujo_y_recibe_el_enlace_igual_que_un_cliente()
    {
        string cliente = CorreoUnico();
        string propietario = CorreoUnico();
        await CrearAsync(baseDatos, cliente);
        await CrearPropietarioAsync(propietario);

        await SolicitarRestablecimientoAsync(cliente);
        await SolicitarRestablecimientoAsync(propietario);

        baseDatos.Remitente.EnlaceDe(cliente).Should().NotBeNullOrEmpty();
        baseDatos.Remitente.EnlaceDe(propietario).Should().NotBeNullOrEmpty();
    }

    private static string TokenDe(string enlace)
    {
        string codigo = QueryHelpers.ParseQuery(new Uri(enlace).Query)["code"].ToString();
        return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(codigo));
    }

    private async Task CrearPropietarioAsync(string correo)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        Resultado<Guid> alta = await ambito.ServiceProvider.GetRequiredService<IServicioCuentas>().CrearPropietarioAsync(
            new Email(correo), new NombrePersona("Ana Garcia"), null, Clave, Ahora, CancellationToken.None);
        alta.EsExito.Should().BeTrue();
    }

    private async Task SolicitarRestablecimientoAsync(string correo)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        SolicitudRestablecimientoClave solicitud = ambito.ServiceProvider.GetRequiredService<SolicitudRestablecimientoClave>();

        await solicitud.SolicitarAsync(correo);
    }

    private async Task SolicitarRegistrandoAsync(string correo, RegistroCaptura<SolicitudRestablecimientoClave> registro)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        IServiceProvider servicios = ambito.ServiceProvider;
        SolicitudRestablecimientoClave solicitud = new(
            servicios.GetRequiredService<UserManager<Usuario>>(),
            servicios.GetRequiredService<IUserConfirmation<Usuario>>(),
            servicios.GetRequiredService<IEmailSender<Usuario>>(),
            servicios.GetRequiredService<IConfiguration>(),
            registro);

        await solicitud.SolicitarAsync(correo);
    }

    private async Task SolicitarConRemitenteRealAsync(
        string correo, IServicioEmail servicioCorreo, RegistroCaptura<SolicitudRestablecimientoClave>? registro = null, string? urlBase = BaseDatosFixture.UrlBase)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        IServiceProvider servicios = ambito.ServiceProvider;
        IConfiguration configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Correo:UrlBase"] = urlBase })
            .Build();
        SolicitudRestablecimientoClave solicitud = new(
            servicios.GetRequiredService<UserManager<Usuario>>(),
            servicios.GetRequiredService<IUserConfirmation<Usuario>>(),
            new RemitenteIdentity(servicioCorreo, servicios.GetRequiredService<IRenderizadorPlantillas>()),
            configuracion,
            registro ?? new RegistroCaptura<SolicitudRestablecimientoClave>());

        await solicitud.SolicitarAsync(correo);
    }
}
