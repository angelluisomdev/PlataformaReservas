using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.CasosUso.Reservas;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
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
        cancelada.CancelarPorEmpresa(DominioPrueba.Ahora.AddHours(-1));
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
}
