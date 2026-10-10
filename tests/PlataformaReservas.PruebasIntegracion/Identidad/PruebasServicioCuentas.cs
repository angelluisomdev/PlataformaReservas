using FluentAssertions;
using Microsoft.AspNetCore.Identity;
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
