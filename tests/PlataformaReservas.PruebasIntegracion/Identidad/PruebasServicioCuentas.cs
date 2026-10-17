using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.PruebasIntegracion.Infraestructura;
using static PlataformaReservas.PruebasIntegracion.Infraestructura.UsuariosPrueba;

namespace PlataformaReservas.PruebasIntegracion.Identidad;

[Collection("BaseDatos")]
public sealed class PruebasServicioCuentas(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Crea_el_propietario_con_su_rol()
    {
        Resultado<Guid> resultado = await CrearPropietarioAsync(CorreoUnico(), Clave);

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        Usuario usuario = (await usuarios.FindByIdAsync(resultado.Valor.ToString()))!;
        resultado.EsExito.Should().BeTrue();
        (await usuarios.IsInRoleAsync(usuario, RolesIdentidad.Propietario)).Should().BeTrue();
    }

    [Fact]
    public async Task Un_correo_ya_registrado_da_conflicto()
    {
        string correo = CorreoUnico();
        await CrearPropietarioAsync(correo, Clave);

        Resultado<Guid> duplicado = await CrearPropietarioAsync(correo.ToUpperInvariant(), Clave);

        duplicado.EsExito.Should().BeFalse();
        duplicado.Codigo.Should().Be(CodigoError.Conflicto);
    }

    [Fact]
    public async Task Una_clave_corta_da_validacion()
    {
        Resultado<Guid> resultado = await CrearPropietarioAsync(CorreoUnico(), "corta");

        resultado.EsExito.Should().BeFalse();
        resultado.Codigo.Should().Be(CodigoError.Validacion);
    }

    [Fact]
    public async Task Renovar_el_sello_de_los_miembros_lo_cambia()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Usuario usuario = await CrearAsync(baseDatos, CorreoUnico());
        await EscenarioEmpresa.GuardarAsync(
            baseDatos, MiembroEmpresa.Crear(Guid.CreateVersion7(), empresa.Id, usuario.Id, RolMiembro.Propietario, Ahora));
        string? antes = await SelloAsync(usuario.Id);

        await using (AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresa.Id))
        {
            await ambito.ServiceProvider.GetRequiredService<IServicioCuentas>()
                .RenovarSelloMiembrosAsync(empresa.Id, CancellationToken.None);
        }

        (await SelloAsync(usuario.Id)).Should().NotBe(antes);
    }

    [Fact]
    public async Task Renovar_el_sello_de_otra_empresa_lanza_y_no_lo_cambia()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Usuario usuario = await CrearAsync(baseDatos, CorreoUnico());
        await EscenarioEmpresa.GuardarAsync(
            baseDatos, MiembroEmpresa.Crear(Guid.CreateVersion7(), ajena.Id, usuario.Id, RolMiembro.Propietario, Ahora));
        string? antes = await SelloAsync(usuario.Id);

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(propia.Id);
        Func<Task> renovar = () => ambito.ServiceProvider.GetRequiredService<IServicioCuentas>()
            .RenovarSelloMiembrosAsync(ajena.Id, CancellationToken.None);

        await renovar.Should().ThrowAsync<InvalidOperationException>();
        (await SelloAsync(usuario.Id)).Should().Be(antes);
    }

    [Fact]
    public async Task El_contacto_devuelve_el_nombre_y_el_telefono_descifrados()
    {
        Usuario usuario = await CrearAsync(baseDatos, CorreoUnico(), "Lucia Fernandez", "611222333");

        (NombrePersona Nombre, Telefono? Telefono)? contacto = await ObtenerContactoAsync(usuario.Id);

        contacto.Should().NotBeNull();
        contacto!.Value.Nombre.Should().Be(new NombrePersona("Lucia Fernandez"));
        contacto.Value.Telefono.Should().Be(new Telefono("611222333"));
    }

    [Fact]
    public async Task El_contacto_de_un_usuario_sin_telefono_no_lleva_telefono()
    {
        Usuario usuario = await CrearAsync(baseDatos, CorreoUnico());

        (NombrePersona Nombre, Telefono? Telefono)? contacto = await ObtenerContactoAsync(usuario.Id);

        contacto.Should().NotBeNull();
        contacto!.Value.Telefono.Should().BeNull();
    }

    [Fact]
    public async Task El_contacto_de_un_usuario_inexistente_no_se_encuentra()
    {
        (await ObtenerContactoAsync(Guid.CreateVersion7())).Should().BeNull();
    }

    [Fact]
    public async Task Establecer_el_telefono_lo_guarda_en_el_perfil_y_cifrado_en_la_tabla()
    {
        Usuario usuario = await CrearAsync(baseDatos, CorreoUnico());

        await using (AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope())
        {
            await ambito.ServiceProvider.GetRequiredService<IServicioCuentas>()
                .EstablecerTelefonoAsync(usuario.Id, new Telefono("644 555 666"), CancellationToken.None);
        }

        (await ObtenerContactoAsync(usuario.Id))!.Value.Telefono.Should().Be(new Telefono("644 555 666"));
        string? enTabla = await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Database
            .SqlQuery<string?>($"""SELECT telefono AS "Value" FROM asp_net_users WHERE id = {usuario.Id}""")
            .SingleAsync());
        enTabla.Should().NotBeNullOrEmpty().And.NotContain("644 555 666");
    }

    [Fact]
    public async Task Establecer_el_telefono_de_un_usuario_inexistente_lanza()
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();

        Func<Task> establecer = () => ambito.ServiceProvider.GetRequiredService<IServicioCuentas>()
            .EstablecerTelefonoAsync(Guid.CreateVersion7(), new Telefono("644555666"), CancellationToken.None);

        await establecer.Should().ThrowAsync<InvalidOperationException>();
    }

    private async Task<(NombrePersona Nombre, Telefono? Telefono)?> ObtenerContactoAsync(Guid usuarioId)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        return await ambito.ServiceProvider.GetRequiredService<IServicioCuentas>()
            .ObtenerContactoAsync(usuarioId, CancellationToken.None);
    }

    private async Task<Resultado<Guid>> CrearPropietarioAsync(string correo, string clave)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        return await ambito.ServiceProvider.GetRequiredService<IServicioCuentas>().CrearPropietarioAsync(
            new Email(correo), new NombrePersona("Ana Garcia"), null, clave, Ahora, CancellationToken.None);
    }

    private async Task<string?> SelloAsync(Guid usuarioId)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        return (await usuarios.FindByIdAsync(usuarioId.ToString()))!.SecurityStamp;
    }
}
