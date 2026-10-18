using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.CasosUso.Cuenta;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos.Cuenta;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Cuenta;

[Collection("BaseDatos")]
public sealed class PruebasConsultarPerfil(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Devuelve_el_nombre_y_el_telefono_descifrados_del_usuario_de_la_sesion()
    {
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico(), "Ana Garcia", "611222333");

        Resultado<PerfilDto> resultado = await ConsultarAsync(usuario.Id);

        resultado.EsExito.Should().BeTrue();
        resultado.Valor.Should().Be(new PerfilDto("Ana Garcia", "611222333"));
    }

    [Fact]
    public async Task Sin_telefono_lo_devuelve_nulo()
    {
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico(), "Ana Garcia", null);

        (await ConsultarAsync(usuario.Id)).Valor!.Telefono.Should().BeNull();
    }

    [Fact]
    public async Task Sin_sesion_da_no_autorizado()
    {
        (await ConsultarAsync(null)).Codigo.Should().Be(CodigoError.NoAutorizado);
    }

    [Fact]
    public async Task Un_usuario_de_la_sesion_inexistente_da_no_encontrado()
    {
        (await ConsultarAsync(Guid.CreateVersion7())).Codigo.Should().Be(CodigoError.NoEncontrado);
    }

    private async Task<Resultado<PerfilDto>> ConsultarAsync(Guid? usuarioId)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(usuarioId);
        return await ambito.ServiceProvider.GetRequiredService<ConsultarPerfil>().EjecutarAsync(CancellationToken.None);
    }
}
