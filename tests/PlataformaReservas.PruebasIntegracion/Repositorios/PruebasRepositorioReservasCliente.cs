using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Repositorios;

[Collection("BaseDatos")]
public sealed class PruebasRepositorioReservasCliente(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Obtiene_la_reserva_propia_con_seguimiento()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva propia = escenario.Reserva(EscenarioReserva.NueveLunesUtc);
        await EscenarioEmpresa.GuardarAsync(baseDatos, propia);

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(escenario.Cliente.Id);
        Reserva? encontrada = await ambito.ServiceProvider.GetRequiredService<IRepositorioReservasCliente>()
            .ObtenerParaModificarAsync(propia.Id, CancellationToken.None);

        encontrada.Should().NotBeNull();
        encontrada!.Id.Should().Be(propia.Id);
        ambito.ServiceProvider.GetRequiredService<ContextoDatos>().Entry(encontrada).State.Should().Be(EntityState.Unchanged);
    }

    [Fact]
    public async Task No_encuentra_la_reserva_de_otro_usuario_ni_una_inexistente()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Usuario otro = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        Reserva deOtro = escenario.Reserva(EscenarioReserva.NueveLunesUtc, otro.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, deOtro);

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(escenario.Cliente.Id);
        IRepositorioReservasCliente repositorio = ambito.ServiceProvider.GetRequiredService<IRepositorioReservasCliente>();

        (await repositorio.ObtenerParaModificarAsync(deOtro.Id, CancellationToken.None)).Should().BeNull();
        (await repositorio.ObtenerParaModificarAsync(Guid.CreateVersion7(), CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Las_futuras_confirmadas_son_solo_las_del_usuario_y_con_seguimiento()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Usuario otro = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        Reserva futura = escenario.Reserva(DominioPrueba.Ahora.AddDays(1));
        Reserva pasada = escenario.Reserva(DominioPrueba.Ahora.AddDays(-1));
        Reserva justoAhora = escenario.Reserva(DominioPrueba.Ahora);
        Reserva cancelada = escenario.Reserva(DominioPrueba.Ahora.AddDays(2));
        cancelada.CancelarPorCliente(DominioPrueba.Ahora, Dominio.Enumeraciones.OrigenCancelacion.Cliente);
        Reserva deOtro = escenario.Reserva(DominioPrueba.Ahora.AddDays(3), otro.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, futura, pasada, justoAhora, cancelada, deOtro);

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(escenario.Cliente.Id);
        IReadOnlyList<Reserva> futuras = await ambito.ServiceProvider.GetRequiredService<IRepositorioReservasCliente>()
            .ObtenerFuturasConfirmadasParaModificarAsync(DominioPrueba.Ahora, CancellationToken.None);

        futuras.Should().ContainSingle().Which.Id.Should().Be(futura.Id);
        ambito.ServiceProvider.GetRequiredService<ContextoDatos>().Entry(futuras[0]).State.Should().Be(EntityState.Unchanged);
    }

    [Fact]
    public async Task Agregar_la_reserva_propia_la_deja_pendiente_de_guardar()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva propia = escenario.Reserva(EscenarioReserva.NueveLunesUtc);

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(escenario.Cliente.Id);
        ambito.ServiceProvider.GetRequiredService<IRepositorioReservasCliente>().Agregar(propia);

        ambito.ServiceProvider.GetRequiredService<ContextoDatos>().Entry(propia).State.Should().Be(EntityState.Added);
    }

    [Fact]
    public async Task Agregar_la_reserva_de_otro_usuario_lanza()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva deOtro = escenario.Reserva(EscenarioReserva.NueveLunesUtc, Guid.CreateVersion7());

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(escenario.Cliente.Id);
        Action agregar = () => ambito.ServiceProvider.GetRequiredService<IRepositorioReservasCliente>().Agregar(deOtro);

        agregar.Should().Throw<InvalidOperationException>();
        ambito.ServiceProvider.GetRequiredService<ContextoDatos>().ChangeTracker.Entries().Should().BeEmpty();
    }
}
