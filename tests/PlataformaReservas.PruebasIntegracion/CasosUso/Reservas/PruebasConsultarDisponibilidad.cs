using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.CasosUso.Reservas;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Disponibilidad;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Reservas;

[Collection("BaseDatos")]
public sealed class PruebasConsultarDisponibilidad(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Devuelve_los_mismos_huecos_que_el_motor_sin_sesion_ni_contexto_de_empresa()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        Resultado<IReadOnlyList<HuecoDisponible>> resultado = await ambito.ServiceProvider.GetRequiredService<ConsultarDisponibilidad>()
            .EjecutarAsync(Comando(escenario, EscenarioReserva.Lunes), CancellationToken.None);
        IReadOnlyList<HuecoDisponible> delMotor = await ambito.ServiceProvider.GetRequiredService<IMotorDisponibilidad>()
            .ObtenerHuecosAsync(escenario.Empresa.Id, escenario.Servicio.Id, escenario.Profesional.Id, EscenarioReserva.Lunes, CancellationToken.None);

        resultado.EsExito.Should().BeTrue();
        resultado.Valor.Should().NotBeEmpty().And.Equal(delMotor);
        resultado.Valor![0].InicioUtc.Should().Be(EscenarioReserva.NueveLunesUtc);
    }

    [Fact]
    public async Task Una_fecha_fuera_de_la_ventana_da_lista_vacia()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        DateOnly fueraDeVentana = EscenarioReserva.Lunes.AddDays(7 * 10);

        (await ConsultarAsync(Comando(escenario, fueraDeVentana))).Should().BeEmpty();
    }

    [Fact]
    public async Task Cualquier_profesional_agrega_los_huecos_de_todos_y_cada_uno_conserva_el_suyo()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Profesional otro = EscenarioReserva.ProfesionalQuePresta(escenario.Empresa.Id, escenario.Servicio.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, otro);

        IReadOnlyList<HuecoDisponible> huecos = await ConsultarAsync(Comando(escenario, EscenarioReserva.Lunes) with { ProfesionalId = null });

        huecos.Select(h => h.ProfesionalId).Distinct().Should().BeEquivalentTo([escenario.Profesional.Id, otro.Id]);
        huecos.Where(h => h.InicioUtc == EscenarioReserva.NueveLunesUtc).Should().HaveCount(2);
        huecos.Select(h => h.InicioUtc).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Un_servicio_de_otra_empresa_da_lista_vacia()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        EscenarioReserva ajeno = await EscenarioReserva.CrearAsync(baseDatos);

        (await ConsultarAsync(Comando(escenario, EscenarioReserva.Lunes) with { ServicioId = ajeno.Servicio.Id })).Should().BeEmpty();
    }

    [Fact]
    public async Task Un_servicio_o_una_empresa_inexistentes_dan_lista_vacia()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);

        (await ConsultarAsync(Comando(escenario, EscenarioReserva.Lunes) with { ServicioId = Guid.CreateVersion7() })).Should().BeEmpty();
        (await ConsultarAsync(Comando(escenario, EscenarioReserva.Lunes) with { EmpresaId = Guid.CreateVersion7() })).Should().BeEmpty();
        (await ConsultarAsync(Comando(escenario, EscenarioReserva.Lunes) with { EmpresaId = Guid.Empty, ServicioId = Guid.Empty })).Should().BeEmpty();
    }

    [Fact]
    public async Task Una_empresa_inactiva_da_lista_vacia()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE empresas SET activa = false WHERE id = {escenario.Empresa.Id}"));

        (await ConsultarAsync(Comando(escenario, EscenarioReserva.Lunes))).Should().BeEmpty();
    }

    [Fact]
    public async Task Dos_consultas_simultaneas_del_mismo_hueco_lo_muestran_disponible_a_ambas()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        ConsultarDisponibilidadComando comando = Comando(escenario, EscenarioReserva.Lunes);

        IReadOnlyList<HuecoDisponible>[] consultas = await Task.WhenAll(
            Task.Run(() => ConsultarAsync(comando)), Task.Run(() => ConsultarAsync(comando)));

        consultas.Should().AllSatisfy(huecos =>
            huecos.Should().Contain(h => h.ProfesionalId == escenario.Profesional.Id && h.InicioUtc == EscenarioReserva.NueveLunesUtc));
        consultas[0].Should().Equal(consultas[1]);
    }

    private static ConsultarDisponibilidadComando Comando(EscenarioReserva escenario, DateOnly fecha)
    {
        return new ConsultarDisponibilidadComando(escenario.Empresa.Id, escenario.Servicio.Id, escenario.Profesional.Id, fecha);
    }

    private async Task<IReadOnlyList<HuecoDisponible>> ConsultarAsync(ConsultarDisponibilidadComando comando)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(null);
        Resultado<IReadOnlyList<HuecoDisponible>> resultado = await ambito.ServiceProvider.GetRequiredService<ConsultarDisponibilidad>()
            .EjecutarAsync(comando, CancellationToken.None);
        resultado.EsExito.Should().BeTrue();
        return resultado.Valor!;
    }
}
