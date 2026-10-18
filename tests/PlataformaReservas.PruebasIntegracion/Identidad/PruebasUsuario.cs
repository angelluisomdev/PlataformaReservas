using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;
using static PlataformaReservas.PruebasIntegracion.Infraestructura.UsuariosPrueba;

namespace PlataformaReservas.PruebasIntegracion.Identidad;

[Collection("BaseDatos")]
public sealed class PruebasUsuario(BaseDatosFixture baseDatos)
{
    private const string Nombre = "Lucia Fernandez";
    private const string Telefono = "699887766";

    [Fact]
    public async Task Un_usuario_creado_se_encuentra_por_correo_con_sus_datos()
    {
        ServiceProvider servicios = baseDatos.ServiciosIdentidad;
        await using AsyncServiceScope ambito = servicios.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        string correo = CorreoUnico();

        IdentityResult resultado = await usuarios.CreateAsync(
            Usuario.Crear(correo, new NombrePersona("Ana Garcia"), new Telefono("600111222"), Ahora), "ClaveSegura1!");
        Usuario? encontrado = await usuarios.FindByEmailAsync(correo);

        resultado.Succeeded.Should().BeTrue();
        encontrado.Should().NotBeNull();
        encontrado!.NombreCompleto.Should().Be("Ana Garcia");
        encontrado.Telefono.Should().Be("600111222");
        encontrado.Activo.Should().BeTrue();
    }

    [Fact]
    public async Task Las_columnas_personales_de_asp_net_users_no_contienen_nada_en_claro()
    {
        string correo = $"lucia.{Guid.CreateVersion7():N}@prueba.es";
        Usuario usuario = await CrearAsync(baseDatos, correo, Nombre, Telefono);

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();
        List<string> columnas = await db.Database.SqlQuery<string>($"""
            SELECT unnest(ARRAY[email, normalized_email, user_name, normalized_user_name, nombre_completo, telefono]) AS "Value"
            FROM asp_net_users WHERE id = {usuario.Id}
            """).ToListAsync();

        columnas.Should().HaveCount(6).And.OnlyContain(valor => !string.IsNullOrEmpty(valor));
        foreach (string valor in columnas)
        {
            valor.Should().NotContainEquivalentOf("lucia")
                .And.NotContainEquivalentOf("prueba.es")
                .And.NotContain(Telefono);
        }
    }

    [Fact]
    public async Task Con_el_cifrado_activo_se_busca_por_correo_y_se_inicia_sesion_pero_no_por_linq()
    {
        string correo = $"{Guid.CreateVersion7():N}@prueba.es";
        await CrearAsync(baseDatos, correo, Nombre, Telefono);

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        SignInManager<Usuario> inicioSesion = ambito.ServiceProvider.GetRequiredService<SignInManager<Usuario>>();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();

        Usuario? encontrado = await usuarios.FindByEmailAsync(correo);
        SignInResult sesion = await inicioSesion.CheckPasswordSignInAsync(encontrado!, Clave, lockoutOnFailure: true);
        List<Usuario> porLinq = await db.Users.Where(u => u.Email == correo).ToListAsync();

        encontrado.Should().NotBeNull();
        encontrado!.NombreCompleto.Should().Be(Nombre);
        sesion.Succeeded.Should().BeTrue();
        porLinq.Should().BeEmpty();
    }

    [Fact]
    public void Crear_deja_un_usuario_activo_con_id_v7_y_los_valores_recibidos()
    {
        Usuario usuario = Usuario.Crear(" ana@prueba.es ", new NombrePersona("Ana Garcia"), null, Ahora);

        usuario.Id.Version.Should().Be(7);
        usuario.Email.Should().Be("ana@prueba.es");
        usuario.UserName.Should().Be("ana@prueba.es");
        usuario.NombreCompleto.Should().Be("Ana Garcia");
        usuario.Telefono.Should().BeNull();
        usuario.Activo.Should().BeTrue();
        usuario.FechaAlta.Should().Be(Ahora);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_rechaza_un_correo_vacio(string correo)
    {
        Action crear = () => Usuario.Crear(correo, new NombrePersona("Ana Garcia"), null, Ahora);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Crear_rechaza_una_fecha_de_alta_que_no_es_utc()
    {
        Action crear = () => Usuario.Crear(
            "ana@prueba.es", new NombrePersona("Ana Garcia"), null, DateTime.SpecifyKind(Ahora, DateTimeKind.Local));

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public async Task EstablecerTelefono_a_un_usuario_sin_telefono_lo_guarda()
    {
        Usuario creado = await CrearAsync(baseDatos, CorreoUnico(), Nombre);

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        Usuario usuario = (await usuarios.FindByIdAsync(creado.Id.ToString()))!;
        usuario.EstablecerTelefono(new Telefono(" 611 222 333 "));
        (await usuarios.UpdateAsync(usuario)).Succeeded.Should().BeTrue();

        await using AsyncServiceScope relectura = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        Usuario releido = (await relectura.ServiceProvider.GetRequiredService<UserManager<Usuario>>()
            .FindByIdAsync(creado.Id.ToString()))!;
        releido.Telefono.Should().Be("611 222 333");
    }

    [Fact]
    public void ActualizarPerfil_cambia_el_nombre_y_el_telefono_y_un_telefono_nulo_lo_quita()
    {
        Usuario usuario = Usuario.Crear("ana@prueba.es", new NombrePersona("Ana Garcia"), new Telefono(Telefono), Ahora);

        usuario.ActualizarPerfil(new NombrePersona("Ana Lopez"), new Telefono("611222333"));
        usuario.NombreCompleto.Should().Be("Ana Lopez");
        usuario.Telefono.Should().Be("611222333");

        usuario.ActualizarPerfil(new NombrePersona("Ana Lopez"), null);
        usuario.Telefono.Should().BeNull();
    }

    [Fact]
    public void DarDeBaja_desactiva_y_repetirla_lanza()
    {
        Usuario usuario = Usuario.Crear("ana@prueba.es", new NombrePersona("Ana Garcia"), null, Ahora);

        usuario.DarDeBaja();
        Action otraVez = usuario.DarDeBaja;

        usuario.Activo.Should().BeFalse();
        otraVez.Should().Throw<DominioException>();
    }

    [Fact]
    public void EstablecerTelefono_a_un_usuario_que_ya_lo_tiene_lanza_y_no_lo_cambia()
    {
        Usuario usuario = Usuario.Crear("ana@prueba.es", new NombrePersona("Ana Garcia"), new Telefono(Telefono), Ahora);

        Action establecer = () => usuario.EstablecerTelefono(new Telefono("611222333"));

        establecer.Should().Throw<DominioException>();
        usuario.Telefono.Should().Be(Telefono);
    }
}
