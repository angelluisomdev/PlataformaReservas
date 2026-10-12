using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.CasosUso.Reservas;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Reservas;

[Collection("BaseDatos")]
public sealed class PruebasConcurrenciaReservas(BaseDatosFixture baseDatos)
{
    private const int Tareas = 10;
    private const int Iteraciones = 5;

    [Fact]
    public async Task Diez_peticiones_simultaneas_sobre_el_mismo_hueco_crean_una_sola_reserva()
    {
        for (int iteracion = 1; iteracion <= Iteraciones; iteracion++)
        {
            EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);

            Resultado<Guid>[] resultados = await Task.WhenAll(
                Enumerable.Range(0, Tareas).Select(_ => Task.Run(() => CrearEnSuPropioAmbitoAsync(escenario))));

            resultados.Count(r => r.EsExito).Should().Be(1, "en la iteracion {0} solo puede ganar una peticion", iteracion);
            resultados.Count(r => r.Codigo == CodigoError.HuecoYaNoDisponible)
                .Should().Be(Tareas - 1, "en la iteracion {0} las demas pierden el hueco", iteracion);
            (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Reservas.CountAsync(r => r.ProfesionalId == escenario.Profesional.Id)))
                .Should().Be(1, "en la iteracion {0} solo queda una fila", iteracion);
        }
    }

    private async Task<Resultado<Guid>> CrearEnSuPropioAmbitoAsync(EscenarioReserva escenario)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(escenario.Cliente.Id);
        return await ambito.ServiceProvider.GetRequiredService<CrearReserva>()
            .EjecutarAsync(escenario.Comando(), CancellationToken.None);
    }
}
