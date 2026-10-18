using FluentAssertions;
using PlataformaReservas.Aplicacion.Dtos.Reservas;
using PlataformaReservas.Dominio.Enumeraciones;

namespace PlataformaReservas.PruebasUnitarias.Dtos.Reservas;

public sealed class PruebasPanelEmpresaDto
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ConvertirADto_copia_los_indicadores_y_ordena_las_dos_listas_por_inicio()
    {
        ReservaEmpresaDto hoyTarde = Reserva(Ahora.AddHours(5));
        ReservaEmpresaDto hoyPronto = Reserva(Ahora.AddHours(1));
        ReservaEmpresaDto pasado = Reserva(Ahora.AddDays(2));
        ReservaEmpresaDto manana = Reserva(Ahora.AddDays(1));

        PanelEmpresaDto dto = PanelEmpresaDto.ConvertirADto(true, true, false, false, false, [hoyTarde, hoyPronto], [pasado, manana]);

        dto.Activa.Should().BeTrue();
        dto.TieneServicioActivo.Should().BeTrue();
        dto.TieneProfesionalActivo.Should().BeFalse();
        dto.TieneAsociacion.Should().BeFalse();
        dto.EsPublicable.Should().BeFalse();
        dto.Hoy.Should().Equal(hoyPronto, hoyTarde);
        dto.Proximas.Should().Equal(manana, pasado);
    }

    [Theory]
    [InlineData(true, false, true, false, true)]
    [InlineData(false, true, false, true, false)]
    public void ConvertirADto_no_cruza_los_indicadores(
        bool activa, bool servicio, bool profesional, bool asociacion, bool publicable)
    {
        PanelEmpresaDto dto = PanelEmpresaDto.ConvertirADto(activa, servicio, profesional, asociacion, publicable, [], []);

        dto.Activa.Should().Be(activa);
        dto.TieneServicioActivo.Should().Be(servicio);
        dto.TieneProfesionalActivo.Should().Be(profesional);
        dto.TieneAsociacion.Should().Be(asociacion);
        dto.EsPublicable.Should().Be(publicable);
    }

    [Fact]
    public void Sin_reservas_las_dos_listas_estan_vacias()
    {
        PanelEmpresaDto dto = PanelEmpresaDto.ConvertirADto(false, true, true, true, true, [], []);

        dto.Activa.Should().BeFalse();
        dto.EsPublicable.Should().BeTrue();
        dto.Hoy.Should().BeEmpty();
        dto.Proximas.Should().BeEmpty();
    }

    private static ReservaEmpresaDto Reserva(DateTime inicio)
    {
        return new ReservaEmpresaDto(
            Guid.CreateVersion7(), inicio, inicio.AddMinutes(30), "Corte", "Ana", "Lucia", "611222333", null,
            EstadoReserva.Confirmada, Cancelable: true, Completable: false);
    }
}
