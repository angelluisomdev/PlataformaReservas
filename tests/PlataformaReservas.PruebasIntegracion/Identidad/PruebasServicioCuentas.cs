using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia;
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
    public async Task El_propietario_tiene_tambien_el_rol_cliente()
    {
        Resultado<Guid> resultado = await CrearPropietarioAsync(CorreoUnico(), Clave);

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        Usuario usuario = (await usuarios.FindByIdAsync(resultado.Valor.ToString()))!;
        (await usuarios.GetRolesAsync(usuario)).Should().BeEquivalentTo([RolesIdentidad.Propietario, RolesIdentidad.Cliente]);
    }

    [Fact]
    public async Task Actualizar_el_perfil_lo_guarda_cifrado()
    {
        Usuario usuario = await CrearAsync(baseDatos, CorreoUnico(), "Ana Garcia", "611222333");

        await using (AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope())
        {
            await ambito.ServiceProvider.GetRequiredService<IServicioCuentas>().ActualizarPerfilAsync(
                usuario.Id, new NombrePersona("Marta Ruiz"), new Telefono("644 555 777"), CancellationToken.None);
        }

        (NombrePersona Nombre, Telefono? Telefono) contacto = (await ObtenerContactoAsync(usuario.Id))!.Value;
        contacto.Nombre.Should().Be(new NombrePersona("Marta Ruiz"));
        contacto.Telefono.Should().Be(new Telefono("644 555 777"));
        List<string?> columnas = await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Database
            .SqlQuery<string?>($"""SELECT unnest(ARRAY[nombre_completo, telefono]) AS "Value" FROM asp_net_users WHERE id = {usuario.Id}""")
            .ToListAsync());
        columnas.Should().OnlyContain(c => c != null && !c.Contains("Marta") && !c.Contains("644 555 777"));
    }

    [Fact]
    public async Task Dar_de_baja_desactiva_y_renueva_el_sello()
    {
        Usuario usuario = await CrearAsync(baseDatos, CorreoUnico());
        string? antes = await SelloAsync(usuario.Id);

        await DarDeBajaAsync(usuario.Id);

        (await EstaActivaAsync(usuario.Id)).Should().BeFalse();
        (await SelloAsync(usuario.Id)).Should().NotBe(antes);
    }

    [Fact]
    public async Task Dar_de_baja_una_cuenta_ya_inactiva_lanza_y_no_renueva_el_sello()
    {
        Usuario usuario = await CrearAsync(baseDatos, CorreoUnico());
        await DesactivarAsync(baseDatos, usuario.Id);
        string? antes = await SelloAsync(usuario.Id);

        Func<Task> darDeBaja = () => DarDeBajaAsync(usuario.Id);

        await darDeBaja.Should().ThrowAsync<DominioException>();
        (await SelloAsync(usuario.Id)).Should().Be(antes);
    }

    [Fact]
    public async Task Dar_de_baja_un_usuario_inexistente_lanza()
    {
        Func<Task> darDeBaja = () => DarDeBajaAsync(Guid.CreateVersion7());

        await darDeBaja.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task EstaActiva_distingue_una_cuenta_activa_de_una_dada_de_baja()
    {
        Usuario activo = await CrearAsync(baseDatos, CorreoUnico());
        Usuario inactivo = await CrearAsync(baseDatos, CorreoUnico());
        await DesactivarAsync(baseDatos, inactivo.Id);

        (await EstaActivaAsync(activo.Id)).Should().BeTrue();
        (await EstaActivaAsync(inactivo.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task EstaActiva_de_un_usuario_inexistente_lanza()
    {
        Func<Task> consultar = () => EstaActivaAsync(Guid.CreateVersion7());

        await consultar.Should().ThrowAsync<InvalidOperationException>();
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
    public async Task Dar_de_baja_registra_el_evento_con_el_identificador_y_sin_el_correo()
    {
        string correo = CorreoUnico();
        Usuario usuario = await CrearAsync(baseDatos, correo);
        RegistroCaptura<ServicioCuentas> registro = new();

        await using (AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope())
        {
            await Cuentas(ambito.ServiceProvider, registro).DarDeBajaAsync(usuario.Id, CancellationToken.None);
        }

        (LogLevel nivel, string linea) = registro.Entradas.Should().ContainSingle().Subject;
        nivel.Should().Be(LogLevel.Information);
        linea.Should().Contain(usuario.Id.ToString()).And.NotContain("@");
    }

    [Fact]
    public async Task Renovar_el_sello_de_los_miembros_registra_el_evento_de_la_empresa()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        RegistroCaptura<ServicioCuentas> registro = new();

        await using (AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresa.Id))
        {
            await Cuentas(ambito.ServiceProvider, registro).RenovarSelloMiembrosAsync(empresa.Id, CancellationToken.None);
        }

        (LogLevel nivel, string linea) = registro.Entradas.Should().ContainSingle().Subject;
        nivel.Should().Be(LogLevel.Information);
        linea.Should().Contain(empresa.Id.ToString());
    }

    [Fact]
    public async Task Renovar_el_sello_de_otra_empresa_registra_un_aviso_con_las_dos_empresas()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        RegistroCaptura<ServicioCuentas> registro = new();

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(propia.Id);
        Func<Task> renovar = () => Cuentas(ambito.ServiceProvider, registro).RenovarSelloMiembrosAsync(ajena.Id, CancellationToken.None);

        await renovar.Should().ThrowAsync<InvalidOperationException>();
        (LogLevel nivel, string linea) = registro.Entradas.Should().ContainSingle().Subject;
        nivel.Should().Be(LogLevel.Warning);
        linea.Should().Contain(propia.Id.ToString()).And.Contain(ajena.Id.ToString());
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

    private static ServicioCuentas Cuentas(IServiceProvider servicios, RegistroCaptura<ServicioCuentas> registro)
    {
        return new ServicioCuentas(
            servicios.GetRequiredService<UserManager<Usuario>>(),
            servicios.GetRequiredService<ContextoDatos>(),
            servicios.GetRequiredService<IContextoEmpresa>(),
            registro);
    }

    private async Task DarDeBajaAsync(Guid usuarioId)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        await ambito.ServiceProvider.GetRequiredService<IServicioCuentas>().DarDeBajaAsync(usuarioId, CancellationToken.None);
    }

    private async Task<bool> EstaActivaAsync(Guid usuarioId)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        return await ambito.ServiceProvider.GetRequiredService<IServicioCuentas>().EstaActivaAsync(usuarioId, CancellationToken.None);
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
