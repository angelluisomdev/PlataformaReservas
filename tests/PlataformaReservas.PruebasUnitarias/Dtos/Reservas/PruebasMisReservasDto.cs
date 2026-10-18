using FluentAssertions;
using PlataformaReservas.Aplicacion.Dtos.Reservas;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Dtos.Reservas;

public sealed class PruebasMisReservasDto
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Proximas_desde_ahora_en_orden_ascendente_e_historicas_en_orden_descendente()
    {
        ReservaClienteDto ayer = Reserva(Ahora.AddDays(-1), EstadoReserva.Completada);
        ReservaClienteDto haceUnaHora = Reserva(Ahora.AddHours(-1), EstadoReserva.Confirmada);
        ReservaClienteDto justoAhora = Reserva(Ahora, EstadoReserva.Confirmada);
        ReservaClienteDto manana = Reserva(Ahora.AddDays(1), EstadoReserva.Confirmada);
        ReservaClienteDto pasadoCancelada = Reserva(Ahora.AddDays(2), EstadoReserva.Cancelada);

        MisReservasDto dto = MisReservasDto.ConvertirADto([manana, ayer, pasadoCancelada, justoAhora, haceUnaHora], Ahora);

        dto.Proximas.Should().Equal(justoAhora, manana, pasadoCancelada);
        dto.Historicas.Should().Equal(haceUnaHora, ayer);
    }

    [Fact]
    public void Cancelables_cuenta_solo_las_confirmadas_que_aun_no_han_empezado_aunque_la_de_ahora_este_en_proximas()
    {
        ReservaClienteDto justoAhora = Reserva(Ahora, EstadoReserva.Confirmada);
        ReservaClienteDto dentroDeUnMinuto = Reserva(Ahora.AddMinutes(1), EstadoReserva.Confirmada);
        ReservaClienteDto manana = Reserva(Ahora.AddDays(1), EstadoReserva.Confirmada);
        ReservaClienteDto mananaCancelada = Reserva(Ahora.AddDays(1), EstadoReserva.Cancelada);
        ReservaClienteDto ayer = Reserva(Ahora.AddDays(-1), EstadoReserva.Confirmada);

        MisReservasDto dto = MisReservasDto.ConvertirADto([justoAhora, dentroDeUnMinuto, manana, mananaCancelada, ayer], Ahora);

        dto.Proximas.Should().Contain(justoAhora);
        dto.Cancelables.Should().Be(2);
    }

    [Fact]
    public void Sin_reservas_las_dos_pestanas_estan_vacias_y_no_hay_cancelables()
    {
        MisReservasDto dto = MisReservasDto.ConvertirADto([], Ahora);

        dto.Proximas.Should().BeEmpty();
        dto.Historicas.Should().BeEmpty();
        dto.Cancelables.Should().Be(0);
    }

    private static ReservaClienteDto Reserva(DateTime inicio, EstadoReserva estado)
    {
        return ReservaClienteDto.ConvertirADto(
            Guid.CreateVersion7(), "Barberia", "Corte", "Ana", new FranjaHoraria(inicio, inicio.AddMinutes(30)), estado, Ahora);
    }
}
