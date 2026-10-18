using FluentAssertions;
using PlataformaReservas.Aplicacion.CasosUso.Reservas;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos.Reservas;
using PlataformaReservas.PruebasUnitarias.Infraestructura;

namespace PlataformaReservas.PruebasUnitarias.CasosUso.Reservas;

public sealed class PruebasConsultarPanelEmpresa
{
    [Fact]
    public async Task Hoy_es_el_dia_local_aunque_en_utc_siga_siendo_el_dia_anterior()
    {
        ConsultasReservasEmpresaFalsas consultas = new();
        DateTime madrugadaLocal = new(2026, 10, 3, 22, 30, 0, DateTimeKind.Utc);

        await new ConsultarPanelEmpresa(consultas, new RelojFijo(madrugadaLocal)).EjecutarAsync(CancellationToken.None);

        consultas.Paneles.Should().ContainSingle().Which.Should().Be((
            new DateTime(2026, 10, 3, 22, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 4, 22, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 11, 22, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public async Task Las_proximas_acaban_al_final_del_septimo_dia_local_aunque_la_semana_cruce_el_cambio_de_hora()
    {
        ConsultasReservasEmpresaFalsas consultas = new();
        DateTime martes = new(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc);

        await new ConsultarPanelEmpresa(consultas, new RelojFijo(martes)).EjecutarAsync(CancellationToken.None);

        consultas.Paneles.Should().ContainSingle().Which.Should().Be((
            new DateTime(2026, 10, 19, 22, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 20, 22, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 27, 23, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public async Task Devuelve_el_panel_de_la_consulta()
    {
        ConsultasReservasEmpresaFalsas consultas = new();

        Resultado<PanelEmpresaDto> resultado = await new ConsultarPanelEmpresa(consultas, new RelojFijo(new DateTime(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc)))
            .EjecutarAsync(CancellationToken.None);

        resultado.EsExito.Should().BeTrue();
        resultado.Valor!.EsPublicable.Should().BeTrue();
    }
}
