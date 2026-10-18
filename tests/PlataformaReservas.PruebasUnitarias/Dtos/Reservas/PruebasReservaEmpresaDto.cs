using FluentAssertions;
using PlataformaReservas.Aplicacion.Dtos.Reservas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Dtos.Reservas;

public sealed class PruebasReservaEmpresaDto
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ConvertirADto_copia_la_franja_los_nombres_y_los_datos_del_cliente()
    {
        Servicio servicio = Servicio.Crear(
            Guid.CreateVersion7(), Guid.CreateVersion7(), new NombreServicio("Corte"), new Duracion(45), new Precio(18.50m), Ahora);
        DateTime inicio = Ahora.AddDays(1);
        Reserva reserva = Reserva.Crear(
            Guid.CreateVersion7(), servicio.EmpresaId, Guid.CreateVersion7(), Guid.CreateVersion7(), servicio, inicio,
            new NombrePersona("Lucia Perez"), new Telefono("611222333"), new Observaciones("Primera visita"), Ahora);

        ReservaEmpresaDto dto = ReservaEmpresaDto.ConvertirADto(reserva, "Corte", "Ana Garcia", Ahora);

        dto.Should().Be(new ReservaEmpresaDto(
            reserva.Id, inicio, inicio.AddMinutes(45), "Corte", "Ana Garcia", "Lucia Perez", "611222333", "Primera visita",
            EstadoReserva.Confirmada, Cancelable: true, Completable: false));
    }

    [Theory]
    [InlineData(EstadoReserva.Confirmada, true)]
    [InlineData(EstadoReserva.Cancelada, false)]
    [InlineData(EstadoReserva.Completada, false)]
    public void Cancelable_solo_si_esta_confirmada_sin_restriccion_temporal(EstadoReserva estado, bool cancelable)
    {
        ReservaEmpresaDto pasada = Dto(estado, minutosHastaFin: -600);
        ReservaEmpresaDto futura = Dto(estado, minutosHastaFin: 600);

        pasada.Cancelable.Should().Be(cancelable);
        futura.Cancelable.Should().Be(cancelable);
    }

    [Theory]
    [InlineData(EstadoReserva.Confirmada, -1, true)]
    [InlineData(EstadoReserva.Confirmada, 0, false)]
    [InlineData(EstadoReserva.Cancelada, -1, false)]
    [InlineData(EstadoReserva.Completada, -1, false)]
    public void Completable_sigue_la_regla_del_dominio(EstadoReserva estado, int minutosHastaFin, bool completable)
    {
        Dto(estado, minutosHastaFin).Completable.Should().Be(completable);
    }

    private static ReservaEmpresaDto Dto(EstadoReserva estado, int minutosHastaFin)
    {
        DateTime fin = Ahora.AddMinutes(minutosHastaFin);
        return ReservaEmpresaDto.ConvertirADto(
            Guid.CreateVersion7(), new FranjaHoraria(fin.AddMinutes(-30), fin), "Corte", "Ana", new NombrePersona("Lucia Perez"),
            new Telefono("611222333"), null, estado, Ahora);
    }
}
