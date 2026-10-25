using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.PruebasIntegracion.Infraestructura;
using static PlataformaReservas.PruebasIntegracion.Infraestructura.UsuariosPrueba;

namespace PlataformaReservas.PruebasIntegracion.Identidad;

[Collection("BaseDatos")]
public sealed class PruebasInicioSesion(BaseDatosFixture baseDatos)
{
    private const string ClaveMala = "ClaveIncorrecta9!";
    private const string Ip = "203.0.113.7";

    [Fact]
    public async Task Con_la_clave_correcta_inicia_sesion()
    {
        string correo = CorreoUnico();
        await CrearAsync(baseDatos, correo);

        (await IniciarAsync(correo, Clave)).Should().BeTrue();
    }

    [Fact]
    public async Task Cinco_fallos_bloquean_la_cuenta_y_la_sexta_con_la_clave_correcta_tambien_falla()
    {
        string correo = CorreoUnico();
        await CrearAsync(baseDatos, correo);

        for (int intento = 0; intento < 5; intento++)
        {
            (await IniciarAsync(correo, ClaveMala)).Should().BeFalse();
        }

        (await IniciarAsync(correo, Clave)).Should().BeFalse();
    }

    [Fact]
    public async Task Cuatro_fallos_no_bloquean_la_cuenta()
    {
        string correo = CorreoUnico();
        await CrearAsync(baseDatos, correo);

        for (int intento = 0; intento < 4; intento++)
        {
            await IniciarAsync(correo, ClaveMala);
        }

        (await IniciarAsync(correo, Clave)).Should().BeTrue();
    }

    [Fact]
    public async Task El_bloqueo_dura_quince_minutos_y_al_vencer_se_vuelve_a_entrar()
    {
        string correo = CorreoUnico();
        Usuario creado = await CrearAsync(baseDatos, correo);
        await BloquearAsync(correo);

        await using (AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope())
        {
            UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
            Usuario usuario = (await usuarios.FindByIdAsync(creado.Id.ToString()))!;
            DateTimeOffset fin = (await usuarios.GetLockoutEndDateAsync(usuario))!.Value;
            fin.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(15), TimeSpan.FromMinutes(1));

            await usuarios.SetLockoutEndDateAsync(usuario, DateTimeOffset.UtcNow.AddSeconds(-1));
        }

        (await IniciarAsync(correo, Clave)).Should().BeTrue();
    }

    [Fact]
    public async Task Cuenta_bloqueada_clave_incorrecta_cuenta_inexistente_e_inactiva_dan_el_mismo_resultado()
    {
        string bloqueada = CorreoUnico();
        string conClaveMala = CorreoUnico();
        string inactiva = CorreoUnico();
        await CrearAsync(baseDatos, bloqueada);
        await CrearAsync(baseDatos, conClaveMala);
        Usuario usuarioInactivo = await CrearAsync(baseDatos, inactiva);
        await DesactivarAsync(baseDatos, usuarioInactivo.Id);
        await BloquearAsync(bloqueada);

        bool[] resultados =
        [
            await IniciarAsync(bloqueada, Clave),
            await IniciarAsync(conClaveMala, ClaveMala),
            await IniciarAsync(CorreoUnico(), Clave),
            await IniciarAsync(inactiva, Clave),
        ];

        resultados.Should().AllSatisfy(r => r.Should().BeFalse());
    }

    [Fact]
    public async Task Un_fallo_registra_un_aviso_con_el_correo_enmascarado_y_la_ip_sin_el_correo_ni_la_clave()
    {
        string correo = CorreoUnico();
        await CrearAsync(baseDatos, correo);
        RegistroCaptura<InicioSesion> registro = new();

        await IniciarAsync(correo, ClaveMala, registro);

        (LogLevel nivel, string linea) = registro.Entradas.Should().ContainSingle().Subject;
        nivel.Should().Be(LogLevel.Warning);
        linea.Should().Contain($"{correo[0]}***@prueba.es").And.Contain(Ip);
        linea.Should().NotContain(correo).And.NotContain(ClaveMala);
    }

    [Fact]
    public async Task Un_intento_sobre_una_cuenta_bloqueada_registra_un_aviso_enmascarado()
    {
        string correo = CorreoUnico();
        await CrearAsync(baseDatos, correo);
        await BloquearAsync(correo);
        RegistroCaptura<InicioSesion> registro = new();

        await IniciarAsync(correo, Clave, registro);

        (LogLevel nivel, string linea) = registro.Entradas.Should().ContainSingle().Subject;
        nivel.Should().Be(LogLevel.Warning);
        linea.Should().Contain("bloqueada").And.Contain(Ip).And.NotContain(correo).And.NotContain(Clave);
    }

    [Fact]
    public async Task Un_acierto_se_registra_como_informacion_sin_datos_personales()
    {
        string correo = CorreoUnico();
        await CrearAsync(baseDatos, correo);
        RegistroCaptura<InicioSesion> registro = new();

        await IniciarAsync(correo, Clave, registro);

        (LogLevel nivel, string linea) = registro.Entradas.Should().ContainSingle().Subject;
        nivel.Should().Be(LogLevel.Information);
        linea.Should().NotContain("@").And.NotContain(Clave);
    }

    private async Task BloquearAsync(string correo)
    {
        for (int intento = 0; intento < 5; intento++)
        {
            await IniciarAsync(correo, ClaveMala);
        }
    }

    private async Task<bool> IniciarAsync(string correo, string clave, RegistroCaptura<InicioSesion>? registro = null)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        IServiceProvider servicios = ambito.ServiceProvider;
        servicios.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext { RequestServices = servicios };
        InicioSesion inicio = new(servicios.GetRequiredService<SignInManager<Usuario>>(), registro ?? new RegistroCaptura<InicioSesion>());

        return await inicio.IniciarAsync(correo, clave, recordar: false, Ip);
    }
}
