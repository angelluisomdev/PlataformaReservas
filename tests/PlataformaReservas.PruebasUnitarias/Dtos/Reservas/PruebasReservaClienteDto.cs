using FluentAssertions;
using PlataformaReservas.Aplicacion.Dtos.Reservas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Dtos.Reservas;

public sealed class PruebasReservaClienteDto
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ConvertirADto_copia_los_nombres_el_inicio_y_el_estado_sin_datos_personales()
    {
        Servicio servicio = Servicio.Crear(
            Guid.CreateVersion7(), Guid.CreateVersion7(), new NombreServicio("Corte"), new Duracion(45), new Precio(18.50m), Ahora);
        DateTime inicio = Ahora.AddDays(1);
        Reserva reserva = Reserva.Crear(
            Guid.CreateVersion7(), servicio.EmpresaId, Guid.CreateVersion7(), Guid.CreateVersion7(), servicio, inicio,
            new NombrePersona("Lucia Perez"), new Telefono("611222333"), null, Ahora);

        ReservaClienteDto dto = ReservaClienteDto.ConvertirADto(reserva, "Barberia Centro", "Corte", "Ana Garcia", Ahora);

        dto.Should().Be(new ReservaClienteDto(
            reserva.Id, "Barberia Centro", "Corte", "Ana Garcia", inicio, EstadoReserva.Confirmada, Cancelable: true));
    }

    [Theory]
    [InlineData(EstadoReserva.Confirmada, 1, true)]
    [InlineData(EstadoReserva.Confirmada, 0, false)]
    [InlineData(EstadoReserva.Cancelada, 60, false)]
    [InlineData(EstadoReserva.Completada, -60, false)]
    public void Cancelable_sigue_la_regla_del_dominio(EstadoReserva estado, int minutosHastaInicio, bool cancelable)
    {
        DateTime inicio = Ahora.AddMinutes(minutosHastaInicio);

        ReservaClienteDto dto = ReservaClienteDto.ConvertirADto(
            Guid.CreateVersion7(), "Barberia", "Corte", "Ana", new FranjaHoraria(inicio, inicio.AddMinutes(30)), estado, Ahora);

        dto.Cancelable.Should().Be(cancelable);
    }
}
