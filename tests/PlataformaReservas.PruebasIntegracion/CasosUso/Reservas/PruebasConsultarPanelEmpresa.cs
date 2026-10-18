using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.CasosUso.Reservas;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos.Reservas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Reservas;

[Collection("BaseDatos")]
public sealed class PruebasConsultarPanelEmpresa(BaseDatosFixture baseDatos)
{
    private static readonly DateTime FinDeHoyUtc = new(2026, 10, 3, 22, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Hoy_son_las_del_dia_local_y_proximas_las_confirmadas_de_los_7_dias_siguientes()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva hoy = escenario.Reserva(DominioPrueba.Ahora.AddHours(2));
        Reserva ultimoMinutoDeHoy = escenario.Reserva(FinDeHoyUtc.AddMinutes(-60));
        Reserva primeraDeManana = escenario.Reserva(FinDeHoyUtc);
        Reserva septimoDia = escenario.Reserva(FinDeHoyUtc.AddDays(7).AddMinutes(-60));
        Reserva octavoDia = escenario.Reserva(FinDeHoyUtc.AddDays(7));
        await EscenarioEmpresa.GuardarAsync(baseDatos, hoy, ultimoMinutoDeHoy, primeraDeManana, septimoDia, octavoDia);

        Resultado<PanelEmpresaDto> resultado = await ConsultarAsync(escenario.Empresa.Id);

        resultado.EsExito.Should().BeTrue();
        resultado.Valor!.Hoy.Select(r => r.Id).Should().Equal(hoy.Id, ultimoMinutoDeHoy.Id);
        resultado.Valor.Proximas.Select(r => r.Id).Should().Equal(primeraDeManana.Id, septimoDia.Id);
        resultado.Valor.EsPublicable.Should().BeTrue();
        resultado.Valor.Activa.Should().BeTrue();
    }

    [Fact]
    public async Task Una_empresa_no_publicable_lo_indica_con_los_requisitos_que_faltan()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        await using (AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope())
        {
            await ambito.ServiceProvider.GetRequiredService<ContextoDatos>().Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM horarios WHERE profesional_id = {escenario.Profesional.Id}");
        }

        PanelEmpresaDto panel = (await ConsultarAsync(escenario.Empresa.Id)).Valor!;

        panel.EsPublicable.Should().BeFalse();
        panel.TieneServicioActivo.Should().BeTrue();
        panel.TieneProfesionalActivo.Should().BeTrue();
        panel.TieneAsociacion.Should().BeTrue();
    }

    [Fact]
    public async Task No_aparece_ninguna_reserva_de_otra_empresa()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        EscenarioReserva otra = await EscenarioReserva.CrearAsync(baseDatos);
        await EscenarioEmpresa.GuardarAsync(
            baseDatos, otra.Reserva(DominioPrueba.Ahora.AddHours(2)), otra.Reserva(DominioPrueba.Ahora.AddDays(2)));

        PanelEmpresaDto panel = (await ConsultarAsync(escenario.Empresa.Id)).Valor!;

        panel.Hoy.Should().BeEmpty();
        panel.Proximas.Should().BeEmpty();
    }

    private async Task<Resultado<PanelEmpresaDto>> ConsultarAsync(Guid empresaId)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        return await ambito.ServiceProvider.GetRequiredService<ConsultarPanelEmpresa>().EjecutarAsync(CancellationToken.None);
    }
}
