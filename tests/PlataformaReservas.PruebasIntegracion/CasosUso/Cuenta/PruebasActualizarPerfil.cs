using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.CasosUso.Cuenta;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Cuenta;

[Collection("BaseDatos")]
public sealed class PruebasActualizarPerfil(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Actualiza_el_nombre_y_el_telefono()
    {
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico(), "Ana Garcia", "611222333");

        Resultado resultado = await ActualizarAsync(usuario.Id, "  Ana Garcia Lopez ", "644 555 666");

        resultado.EsExito.Should().BeTrue();
        (NombrePersona Nombre, Telefono? Telefono) contacto = await ContactoAsync(usuario.Id);
        contacto.Nombre.Should().Be(new NombrePersona("Ana Garcia Lopez"));
        contacto.Telefono.Should().Be(new Telefono("644 555 666"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Un_telefono_en_blanco_lo_quita(string? telefono)
    {
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico(), "Ana Garcia", "611222333");

        (await ActualizarAsync(usuario.Id, "Ana Garcia", telefono)).EsExito.Should().BeTrue();

        (await ContactoAsync(usuario.Id)).Telefono.Should().BeNull();
    }

    [Theory]
    [InlineData("", "611222333")]
    [InlineData("Ana Garcia", "12345")]
    [InlineData("Ana Garcia", "seis uno uno")]
    public async Task Un_dato_invalido_da_validacion_y_no_cambia_nada(string nombre, string telefono)
    {
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico(), "Lucia Perez", "611222333");

        (await ActualizarAsync(usuario.Id, nombre, telefono)).Codigo.Should().Be(CodigoError.Validacion);

        (NombrePersona Nombre, Telefono? Telefono) contacto = await ContactoAsync(usuario.Id);
        contacto.Nombre.Should().Be(new NombrePersona("Lucia Perez"));
        contacto.Telefono.Should().Be(new Telefono("611222333"));
    }

    [Fact]
    public async Task Sin_sesion_da_no_autorizado()
    {
        (await ActualizarAsync(null, "Ana Garcia", null)).Codigo.Should().Be(CodigoError.NoAutorizado);
    }

    [Fact]
    public async Task Editar_el_perfil_no_altera_las_copias_de_una_reserva_ya_creada()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva reserva = escenario.Reserva(DominioPrueba.Ahora.AddDays(1));
        await EscenarioEmpresa.GuardarAsync(baseDatos, reserva);

        await ActualizarAsync(escenario.Cliente.Id, "Otro Nombre", "699000111");

        Reserva guardada = await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Reservas.SingleAsync(r => r.Id == reserva.Id));
        guardada.NombreCliente.Should().Be(new NombrePersona("Lucia Fernandez"));
        guardada.TelefonoCliente.Should().Be(new Telefono(EscenarioReserva.TelefonoCliente));
    }

    private async Task<Resultado> ActualizarAsync(Guid? usuarioId, string? nombre, string? telefono)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(usuarioId);
        return await ambito.ServiceProvider.GetRequiredService<ActualizarPerfil>()
            .EjecutarAsync(new ActualizarPerfilComando(nombre, telefono), CancellationToken.None);
    }

    private async Task<(NombrePersona Nombre, Telefono? Telefono)> ContactoAsync(Guid usuarioId)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        return (await ambito.ServiceProvider.GetRequiredService<IServicioCuentas>()
            .ObtenerContactoAsync(usuarioId, CancellationToken.None))!.Value;
    }
}
