using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.CasosUso.Reservas;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Disponibilidad;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.Eventos;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Reservas;

[Collection("BaseDatos")]
public sealed class PruebasCancelarReservaEmpresa(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Cancela_una_reserva_futura_de_la_empresa_y_registra_la_fecha()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva reserva = escenario.Reserva(EscenarioReserva.NueveLunesUtc);
        await EscenarioEmpresa.GuardarAsync(baseDatos, reserva);

        Resultado resultado = await CancelarAsync(escenario.Empresa.Id, reserva.Id);

        resultado.EsExito.Should().BeTrue();
        Reserva cancelada = await LeerReservaAsync(reserva.Id);
        cancelada.Estado.Should().Be(EstadoReserva.Cancelada);
        cancelada.FechaCancelacion.Should().Be(DominioPrueba.Ahora);
        cancelada.FechaModificacion.Should().Be(DominioPrueba.Ahora);
    }

    [Fact]
    public async Task Cancela_una_reserva_confirmada_ya_pasada()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva pasada = escenario.Reserva(DominioPrueba.Ahora.AddDays(-1));
        await EscenarioEmpresa.GuardarAsync(baseDatos, pasada);

        Resultado resultado = await CancelarAsync(escenario.Empresa.Id, pasada.Id);

        resultado.EsExito.Should().BeTrue();
        (await LeerReservaAsync(pasada.Id)).Estado.Should().Be(EstadoReserva.Cancelada);
    }

    [Fact]
    public async Task Una_reserva_de_otra_empresa_no_se_encuentra_y_queda_intacta()
    {
        EscenarioReserva propia = await EscenarioReserva.CrearAsync(baseDatos);
        EscenarioReserva ajena = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva deLaAjena = ajena.Reserva(EscenarioReserva.NueveLunesUtc);
        await EscenarioEmpresa.GuardarAsync(baseDatos, deLaAjena);

        Resultado resultado = await CancelarAsync(propia.Empresa.Id, deLaAjena.Id);

        resultado.Codigo.Should().Be(CodigoError.NoEncontrado);
        (await LeerReservaAsync(deLaAjena.Id)).Estado.Should().Be(EstadoReserva.Confirmada);
    }

    [Fact]
    public async Task Una_reserva_inexistente_no_se_encuentra()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);

        Resultado resultado = await CancelarAsync(escenario.Empresa.Id, Guid.CreateVersion7());

        resultado.Codigo.Should().Be(CodigoError.NoEncontrado);
    }

    [Fact]
    public async Task Una_reserva_cancelada_da_estado_no_valido()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva cancelada = escenario.Reserva(EscenarioReserva.NueveLunesUtc);
        cancelada.CancelarPorEmpresa(DominioPrueba.Ahora.AddHours(-1), OrigenCancelacion.Empresa);
        await EscenarioEmpresa.GuardarAsync(baseDatos, cancelada);

        Resultado resultado = await CancelarAsync(escenario.Empresa.Id, cancelada.Id);

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

        Resultado resultado = await CancelarAsync(escenario.Empresa.Id, completada.Id);

        resultado.Codigo.Should().Be(CodigoError.EstadoNoValido);
        (await LeerReservaAsync(completada.Id)).Estado.Should().Be(EstadoReserva.Completada);
    }

    [Fact]
    public async Task El_hueco_que_cancela_la_empresa_se_vuelve_a_ofrecer_y_se_puede_volver_a_reservar()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Resultado<Guid> primera = await ReservarAsync(escenario);
        primera.EsExito.Should().BeTrue();

        (await CancelarAsync(escenario.Empresa.Id, primera.Valor)).EsExito.Should().BeTrue();

        await using (AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope())
        {
            IReadOnlyList<HuecoDisponible> huecos = await ambito.ServiceProvider.GetRequiredService<IMotorDisponibilidad>()
                .ObtenerHuecosAsync(
                    escenario.Empresa.Id, escenario.Servicio.Id, escenario.Profesional.Id, EscenarioReserva.Lunes, CancellationToken.None);
            huecos.Should().Contain(h => h.ProfesionalId == escenario.Profesional.Id && h.InicioUtc == EscenarioReserva.NueveLunesUtc);
        }

        Resultado<Guid> segunda = await ReservarAsync(escenario);
        segunda.EsExito.Should().BeTrue();
        (await LeerReservaAsync(segunda.Valor)).Estado.Should().Be(EstadoReserva.Confirmada);
    }

    private async Task<Resultado<Guid>> ReservarAsync(EscenarioReserva escenario)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(escenario.Cliente.Id);
        return await ambito.ServiceProvider.GetRequiredService<CrearReserva>().EjecutarAsync(escenario.Comando(), CancellationToken.None);
    }

    private async Task<Resultado> CancelarAsync(Guid empresaId, Guid reservaId)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        return await ambito.ServiceProvider.GetRequiredService<CancelarReservaEmpresa>()
            .EjecutarAsync(new CancelarReservaEmpresaComando(reservaId), CancellationToken.None);
    }

    private Task<Reserva> LeerReservaAsync(Guid id)
    {
        return EscenarioEmpresa.LeerAsync(baseDatos, db => db.Reservas.SingleAsync(r => r.Id == id));
    }

    [Fact]
    public async Task Tras_cancelar_publica_la_cancelacion_con_origen_empresa()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva reserva = escenario.Reserva(EscenarioReserva.NueveLunesUtc);
        await EscenarioEmpresa.GuardarAsync(baseDatos, reserva);

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(escenario.Empresa.Id);
        Resultado resultado = await ambito.ServiceProvider.GetRequiredService<CancelarReservaEmpresa>()
            .EjecutarAsync(new CancelarReservaEmpresaComando(reserva.Id), CancellationToken.None);

        resultado.EsExito.Should().BeTrue();
        ambito.ServiceProvider.GetRequiredService<PublicadorCaptura>().Publicados.Should().ContainSingle().Which.Should()
            .Be(new ReservaCancelada(reserva.Id, OrigenCancelacion.Empresa, DominioPrueba.Ahora));
        EscenarioEmpresa.EntidadSeguida<Reserva>(ambito.ServiceProvider, reserva.Id).Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Una_cancelacion_fallida_no_publica_nada()
    {
        EscenarioReserva propia = await EscenarioReserva.CrearAsync(baseDatos);
        EscenarioReserva ajena = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva deLaAjena = ajena.Reserva(EscenarioReserva.NueveLunesUtc);
        await EscenarioEmpresa.GuardarAsync(baseDatos, deLaAjena);

        (Resultado resultado, IReadOnlyList<IEventoDominio> publicados) = await CancelarYCapturarAsync(propia.Empresa.Id, deLaAjena.Id);

        resultado.Codigo.Should().Be(CodigoError.NoEncontrado);
        publicados.Should().BeEmpty();
    }

    private async Task<(Resultado Resultado, IReadOnlyList<IEventoDominio> Publicados)> CancelarYCapturarAsync(
        Guid empresaId, Guid reservaId)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        Resultado resultado = await ambito.ServiceProvider.GetRequiredService<CancelarReservaEmpresa>()
            .EjecutarAsync(new CancelarReservaEmpresaComando(reservaId), CancellationToken.None);
        return (resultado, ambito.ServiceProvider.GetRequiredService<PublicadorCaptura>().Publicados);
    }
}
