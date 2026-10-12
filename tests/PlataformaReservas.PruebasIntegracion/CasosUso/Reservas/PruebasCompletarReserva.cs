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
public sealed class PruebasCompletarReserva(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Completa_una_reserva_confirmada_ya_terminada()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva terminada = escenario.Reserva(DominioPrueba.Ahora.AddHours(-2));
        await EscenarioEmpresa.GuardarAsync(baseDatos, terminada);

        Resultado resultado = await CompletarAsync(escenario.Empresa.Id, terminada.Id);

        resultado.EsExito.Should().BeTrue();
        Reserva completada = await LeerReservaAsync(terminada.Id);
        completada.Estado.Should().Be(EstadoReserva.Completada);
        completada.FechaCancelacion.Should().BeNull();
    }

    [Theory]
    [InlineData(-45)]
    [InlineData(60)]
    public async Task Una_reserva_que_no_ha_terminado_da_estado_no_valido(int minutosDesdeAhora)
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva sinTerminar = escenario.Reserva(DominioPrueba.Ahora.AddMinutes(minutosDesdeAhora));
        await EscenarioEmpresa.GuardarAsync(baseDatos, sinTerminar);

        Resultado resultado = await CompletarAsync(escenario.Empresa.Id, sinTerminar.Id);

        resultado.Codigo.Should().Be(CodigoError.EstadoNoValido);
        (await LeerReservaAsync(sinTerminar.Id)).Estado.Should().Be(EstadoReserva.Confirmada);
    }

    [Fact]
    public async Task Una_reserva_cancelada_da_estado_no_valido()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva cancelada = escenario.Reserva(DominioPrueba.Ahora.AddHours(-2));
        cancelada.CancelarPorEmpresa(DominioPrueba.Ahora.AddHours(-3));
        await EscenarioEmpresa.GuardarAsync(baseDatos, cancelada);

        Resultado resultado = await CompletarAsync(escenario.Empresa.Id, cancelada.Id);

        resultado.Codigo.Should().Be(CodigoError.EstadoNoValido);
        (await LeerReservaAsync(cancelada.Id)).Estado.Should().Be(EstadoReserva.Cancelada);
    }

    [Fact]
    public async Task Una_reserva_de_otra_empresa_no_se_encuentra_y_queda_intacta()
    {
        EscenarioReserva propia = await EscenarioReserva.CrearAsync(baseDatos);
        EscenarioReserva ajena = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva deLaAjena = ajena.Reserva(DominioPrueba.Ahora.AddHours(-2));
        await EscenarioEmpresa.GuardarAsync(baseDatos, deLaAjena);

        Resultado resultado = await CompletarAsync(propia.Empresa.Id, deLaAjena.Id);

        resultado.Codigo.Should().Be(CodigoError.NoEncontrado);
        (await LeerReservaAsync(deLaAjena.Id)).Estado.Should().Be(EstadoReserva.Confirmada);
    }

    [Fact]
    public async Task Una_reserva_inexistente_no_se_encuentra()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);

        Resultado resultado = await CompletarAsync(escenario.Empresa.Id, Guid.CreateVersion7());

        resultado.Codigo.Should().Be(CodigoError.NoEncontrado);
    }

    private async Task<Resultado> CompletarAsync(Guid empresaId, Guid reservaId)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        return await ambito.ServiceProvider.GetRequiredService<CompletarReserva>()
            .EjecutarAsync(new CompletarReservaComando(reservaId), CancellationToken.None);
    }

    private Task<Reserva> LeerReservaAsync(Guid id)
    {
        return EscenarioEmpresa.LeerAsync(baseDatos, db => db.Reservas.SingleAsync(r => r.Id == id));
    }
}
