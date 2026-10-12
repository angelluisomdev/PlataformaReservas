using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.CasosUso.Reservas;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Disponibilidad;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.Eventos;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Reservas;

[Collection("BaseDatos")]
public sealed class PruebasCancelarReservaCliente(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Cancela_la_reserva_propia_y_registra_la_fecha_sin_borrarla()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva reserva = escenario.Reserva(EscenarioReserva.NueveLunesUtc);
        await EscenarioEmpresa.GuardarAsync(baseDatos, reserva);

        Resultado resultado = await CancelarAsync(escenario.Cliente.Id, reserva.Id);

        resultado.EsExito.Should().BeTrue();
        Reserva cancelada = await LeerReservaAsync(reserva.Id);
        cancelada.Estado.Should().Be(EstadoReserva.Cancelada);
        cancelada.FechaCancelacion.Should().Be(DominioPrueba.Ahora);
        cancelada.FechaModificacion.Should().Be(DominioPrueba.Ahora);
    }

    [Fact]
    public async Task La_reserva_de_otro_usuario_no_se_encuentra_y_queda_intacta()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Usuario otro = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        Reserva deOtro = escenario.Reserva(EscenarioReserva.NueveLunesUtc, otro.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, deOtro);

        Resultado resultado = await CancelarAsync(escenario.Cliente.Id, deOtro.Id);

        resultado.Codigo.Should().Be(CodigoError.NoEncontrado);
        Reserva intacta = await LeerReservaAsync(deOtro.Id);
        intacta.Estado.Should().Be(EstadoReserva.Confirmada);
        intacta.FechaCancelacion.Should().BeNull();
    }

    [Fact]
    public async Task Una_reserva_inexistente_no_se_encuentra()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);

        Resultado resultado = await CancelarAsync(escenario.Cliente.Id, Guid.CreateVersion7());

        resultado.Codigo.Should().Be(CodigoError.NoEncontrado);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task Una_reserva_ya_iniciada_da_estado_no_valido(int minutosDesdeAhora)
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva iniciada = escenario.Reserva(DominioPrueba.Ahora.AddMinutes(minutosDesdeAhora));
        await EscenarioEmpresa.GuardarAsync(baseDatos, iniciada);

        Resultado resultado = await CancelarAsync(escenario.Cliente.Id, iniciada.Id);

        resultado.Codigo.Should().Be(CodigoError.EstadoNoValido);
        (await LeerReservaAsync(iniciada.Id)).Estado.Should().Be(EstadoReserva.Confirmada);
    }

    [Fact]
    public async Task Una_reserva_cancelada_da_estado_no_valido()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva cancelada = escenario.Reserva(EscenarioReserva.NueveLunesUtc);
        cancelada.CancelarPorEmpresa(DominioPrueba.Ahora.AddHours(-1), OrigenCancelacion.Empresa);
        await EscenarioEmpresa.GuardarAsync(baseDatos, cancelada);

        Resultado resultado = await CancelarAsync(escenario.Cliente.Id, cancelada.Id);

        resultado.Codigo.Should().Be(CodigoError.EstadoNoValido);
        (await LeerReservaAsync(cancelada.Id)).FechaCancelacion.Should().Be(DominioPrueba.Ahora.AddHours(-1));
    }

    [Fact]
    public async Task Una_reserva_completada_da_estado_no_valido()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva completada = escenario.Reserva(DominioPrueba.Ahora.AddHours(-2));
        completada.Completar(DominioPrueba.Ahora);
        await EscenarioEmpresa.GuardarAsync(baseDatos, completada);

        Resultado resultado = await CancelarAsync(escenario.Cliente.Id, completada.Id);

        resultado.Codigo.Should().Be(CodigoError.EstadoNoValido);
        (await LeerReservaAsync(completada.Id)).Estado.Should().Be(EstadoReserva.Completada);
    }

    [Fact]
    public async Task Sin_usuario_da_no_autorizado()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva reserva = escenario.Reserva(EscenarioReserva.NueveLunesUtc);
        await EscenarioEmpresa.GuardarAsync(baseDatos, reserva);

        Resultado resultado = await CancelarAsync(null, reserva.Id);

        resultado.Codigo.Should().Be(CodigoError.NoAutorizado);
        (await LeerReservaAsync(reserva.Id)).Estado.Should().Be(EstadoReserva.Confirmada);
    }

    [Fact]
    public async Task El_hueco_cancelado_se_vuelve_a_ofrecer_y_se_puede_volver_a_reservar()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Resultado<Guid> primera = await CrearAsync(escenario);
        primera.EsExito.Should().BeTrue();

        (await CancelarAsync(escenario.Cliente.Id, primera.Valor)).EsExito.Should().BeTrue();

        IReadOnlyList<HuecoDisponible> huecos = await HuecosDelLunesAsync(escenario);
        huecos.Should().Contain(h => h.ProfesionalId == escenario.Profesional.Id && h.InicioUtc == EscenarioReserva.NueveLunesUtc);
        Resultado<Guid> segunda = await CrearAsync(escenario);
        segunda.EsExito.Should().BeTrue();
        (await LeerReservaAsync(segunda.Valor)).Estado.Should().Be(EstadoReserva.Confirmada);
    }

    private async Task<Resultado> CancelarAsync(Guid? usuarioId, Guid reservaId)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(usuarioId);
        return await ambito.ServiceProvider.GetRequiredService<CancelarReservaCliente>()
            .EjecutarAsync(new CancelarReservaClienteComando(reservaId), CancellationToken.None);
    }

    private async Task<Resultado<Guid>> CrearAsync(EscenarioReserva escenario)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(escenario.Cliente.Id);
        return await ambito.ServiceProvider.GetRequiredService<CrearReserva>()
            .EjecutarAsync(escenario.Comando(), CancellationToken.None);
    }

    private async Task<IReadOnlyList<HuecoDisponible>> HuecosDelLunesAsync(EscenarioReserva escenario)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        return await ambito.ServiceProvider.GetRequiredService<IMotorDisponibilidad>().ObtenerHuecosAsync(
            escenario.Empresa.Id, escenario.Servicio.Id, escenario.Profesional.Id, EscenarioReserva.Lunes, CancellationToken.None);
    }

    private Task<Reserva> LeerReservaAsync(Guid id)
    {
        return EscenarioEmpresa.LeerAsync(baseDatos, db => db.Reservas.SingleAsync(r => r.Id == id));
    }

    [Fact]
    public async Task Tras_cancelar_publica_la_cancelacion_con_origen_cliente()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva reserva = escenario.Reserva(EscenarioReserva.NueveLunesUtc);
        await EscenarioEmpresa.GuardarAsync(baseDatos, reserva);

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(escenario.Cliente.Id);
        Resultado resultado = await ambito.ServiceProvider.GetRequiredService<CancelarReservaCliente>()
            .EjecutarAsync(new CancelarReservaClienteComando(reserva.Id), CancellationToken.None);

        resultado.EsExito.Should().BeTrue();
        ambito.ServiceProvider.GetRequiredService<PublicadorCaptura>().Publicados.Should().ContainSingle().Which.Should()
            .Be(new ReservaCancelada(reserva.Id, OrigenCancelacion.Cliente, DominioPrueba.Ahora));
        EscenarioEmpresa.EntidadSeguida<Reserva>(ambito.ServiceProvider, reserva.Id).Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Una_cancelacion_fallida_no_publica_nada()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva iniciada = escenario.Reserva(DominioPrueba.Ahora.AddMinutes(-10));
        await EscenarioEmpresa.GuardarAsync(baseDatos, iniciada);

        (Resultado inexistente, IReadOnlyList<IEventoDominio> trasNoEncontrada) =
            await CancelarYCapturarAsync(escenario.Cliente.Id, Guid.CreateVersion7());
        (Resultado yaIniciada, IReadOnlyList<IEventoDominio> trasEstadoNoValido) =
            await CancelarYCapturarAsync(escenario.Cliente.Id, iniciada.Id);

        inexistente.Codigo.Should().Be(CodigoError.NoEncontrado);
        yaIniciada.Codigo.Should().Be(CodigoError.EstadoNoValido);
        trasNoEncontrada.Should().BeEmpty();
        trasEstadoNoValido.Should().BeEmpty();
    }

    private async Task<(Resultado Resultado, IReadOnlyList<IEventoDominio> Publicados)> CancelarYCapturarAsync(
        Guid usuarioId, Guid reservaId)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(usuarioId);
        Resultado resultado = await ambito.ServiceProvider.GetRequiredService<CancelarReservaCliente>()
            .EjecutarAsync(new CancelarReservaClienteComando(reservaId), CancellationToken.None);
        return (resultado, ambito.ServiceProvider.GetRequiredService<PublicadorCaptura>().Publicados);
    }
}
