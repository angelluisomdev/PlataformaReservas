using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Dominio.Eventos;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;
using static PlataformaReservas.PruebasIntegracion.Infraestructura.UsuariosPrueba;

namespace PlataformaReservas.PruebasIntegracion.Identidad;

[Collection("BaseDatos")]
public sealed class PruebasRegistroCliente(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task El_registro_crea_un_cliente_con_su_rol()
    {
        string correo = CorreoUnico();

        (IdentityResult resultado, Usuario? usuario) = await RegistrarAsync(correo);

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        Usuario guardado = (await usuarios.FindByIdAsync(usuario!.Id.ToString()))!;
        resultado.Succeeded.Should().BeTrue();
        (await usuarios.IsInRoleAsync(guardado, RolesIdentidad.Cliente)).Should().BeTrue();
    }

    [Fact]
    public async Task Registrar_un_telefono_mal_formado_falla_sin_excepcion_ni_usuario()
    {
        string correo = CorreoUnico();

        (IdentityResult resultado, Usuario? usuario) = await RegistrarAsync(correo, "600 abc 000");

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        resultado.Succeeded.Should().BeFalse();
        usuario.Should().BeNull();
        (await usuarios.FindByEmailAsync(correo)).Should().BeNull();
    }

    [Fact]
    public async Task Registrar_un_correo_duplicado_falla_con_el_cifrado_activo()
    {
        string correo = CorreoUnico();
        await RegistrarAsync(correo);

        (IdentityResult duplicado, _) = await RegistrarAsync(correo.ToUpperInvariant());

        duplicado.Succeeded.Should().BeFalse();
        duplicado.Errors.Should().Contain(e => e.Code == "DuplicateEmail");
    }

    [Fact]
    public async Task Registrar_un_nombre_vacio_falla_sin_excepcion_ni_usuario()
    {
        string correo = CorreoUnico();

        (IdentityResult resultado, Usuario? usuario) = await RegistrarAsync(correo, nombre: "   ");

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        resultado.Succeeded.Should().BeFalse();
        usuario.Should().BeNull();
        (await usuarios.FindByEmailAsync(correo)).Should().BeNull();
    }

    [Fact]
    public async Task Registrar_sin_telefono_crea_el_usuario_sin_telefono()
    {
        string correo = CorreoUnico();

        (IdentityResult resultado, _) = await RegistrarAsync(correo);

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        resultado.Succeeded.Should().BeTrue();
        (await usuarios.FindByEmailAsync(correo))!.Telefono.Should().BeNull();
    }

    [Fact]
    public async Task Tras_crear_la_cuenta_publica_CuentaRegistrada_con_su_identificador()
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        RegistroCliente registro = ambito.ServiceProvider.GetRequiredService<RegistroCliente>();

        (IdentityResult resultado, Usuario? usuario) = await registro.RegistrarAsync(CorreoUnico(), "Ana Garcia", null, Clave, Ahora, CancellationToken.None);

        resultado.Succeeded.Should().BeTrue();
        ambito.ServiceProvider.GetRequiredService<PublicadorCaptura>().Publicados
            .Should().ContainSingle().Which.Should().Be(new CuentaRegistrada(usuario!.Id, Ahora));
    }

    [Fact]
    public async Task CuentaRegistrada_se_publica_cuando_el_usuario_ya_es_legible_desde_otro_ambito()
    {
        string correo = CorreoUnico();
        bool? legibleAlPublicar = null;
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        PublicadorQueConsulta publicador = new(async () =>
        {
            await using AsyncServiceScope otro = baseDatos.ServiciosIdentidad.CreateAsyncScope();
            legibleAlPublicar = await otro.ServiceProvider.GetRequiredService<UserManager<Usuario>>().FindByEmailAsync(correo) is not null;
        });
        RegistroCliente registro = new(
            ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>(),
            ambito.ServiceProvider.GetRequiredService<ContextoDatos>(),
            publicador);

        (IdentityResult resultado, _) = await registro.RegistrarAsync(correo, "Ana Garcia", null, Clave, Ahora, CancellationToken.None);

        resultado.Succeeded.Should().BeTrue();
        legibleAlPublicar.Should().BeTrue("el evento se publica despues del commit, cuando el alta ya es visible para otros");
    }

    [Fact]
    public async Task Un_registro_fallido_no_publica_nada()
    {
        string correo = CorreoUnico();
        await RegistrarAsync(correo);
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        RegistroCliente registro = ambito.ServiceProvider.GetRequiredService<RegistroCliente>();

        (IdentityResult duplicado, _) = await registro.RegistrarAsync(correo, "Ana Garcia", null, Clave, Ahora, CancellationToken.None);
        (IdentityResult invalido, _) = await registro.RegistrarAsync(CorreoUnico(), "Ana Garcia", "600 abc 000", Clave, Ahora, CancellationToken.None);

        duplicado.Succeeded.Should().BeFalse();
        invalido.Succeeded.Should().BeFalse();
        ambito.ServiceProvider.GetRequiredService<PublicadorCaptura>().Publicados.Should().BeEmpty();
    }

    private sealed class PublicadorQueConsulta(Func<Task> alPublicar) : IPublicadorEventos
    {
        public async ValueTask PublicarAsync(IReadOnlyCollection<IEventoDominio> eventos, CancellationToken ct)
        {
            await alPublicar();
        }
    }

    private async Task<(IdentityResult Resultado, Usuario? Usuario)> RegistrarAsync(
        string correo, string? telefono = null, string nombre = "Ana Garcia")
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        RegistroCliente registro = ambito.ServiceProvider.GetRequiredService<RegistroCliente>();

        return await registro.RegistrarAsync(correo, nombre, telefono, Clave, Ahora, CancellationToken.None);
    }
}
