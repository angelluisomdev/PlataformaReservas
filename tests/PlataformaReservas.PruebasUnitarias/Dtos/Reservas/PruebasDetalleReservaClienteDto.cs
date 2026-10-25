using FluentAssertions;
using PlataformaReservas.Aplicacion.Dtos.Reservas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Dtos.Reservas;

public sealed class PruebasDetalleReservaClienteDto
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ConvertirADto_copia_los_datos_aplicados_y_es_cancelable_si_esta_confirmada_y_es_futura()
    {
        Reserva reserva = CrearReserva(Ahora.AddHours(2), new Observaciones("Llegare puntual"));

        DetalleReservaClienteDto dto = DetalleReservaClienteDto.ConvertirADto(reserva, "Barberia Centro", "Corte", "Ana Garcia", Ahora);

        dto.Should().Be(new DetalleReservaClienteDto(
            reserva.Id, "Barberia Centro", "Corte", "Ana Garcia", Ahora.AddHours(2), Ahora.AddHours(2).AddMinutes(45),
            45, 18.50m, "Llegare puntual", EstadoReserva.Confirmada, true, null));
    }

    [Fact]
    public void Una_reserva_que_empieza_justo_ahora_no_es_cancelable()
    {
        Reserva reserva = CrearReserva(Ahora, null);

        DetalleReservaClienteDto dto = DetalleReservaClienteDto.ConvertirADto(reserva, "E", "S", "P", Ahora);

        dto.Cancelable.Should().BeFalse();
        dto.MotivoNoCancelable.Should().Be(
            "La reserva ya ha comenzado y no se puede cancelar desde aquí. Contacta con el establecimiento.");
    }

    [Fact]
    public void Una_reserva_cancelada_no_es_cancelable_aunque_sea_futura()
    {
        Reserva reserva = CrearReserva(Ahora.AddHours(2), null);
        reserva.CancelarPorCliente(Ahora, OrigenCancelacion.Cliente);

        DetalleReservaClienteDto dto = DetalleReservaClienteDto.ConvertirADto(reserva, "E", "S", "P", Ahora);

        dto.Cancelable.Should().BeFalse();
        dto.Estado.Should().Be(EstadoReserva.Cancelada);
        dto.Observaciones.Should().BeNull();
        dto.MotivoNoCancelable.Should().Be("Esta reserva está cancelada y ya no se puede cancelar.");
    }

    [Fact]
    public void Una_reserva_completada_no_es_cancelable_y_explica_el_motivo()
    {
        Reserva reserva = CrearReserva(Ahora.AddHours(-2), null);
        reserva.Completar(Ahora);

        DetalleReservaClienteDto dto = DetalleReservaClienteDto.ConvertirADto(reserva, "E", "S", "P", Ahora);

        dto.Cancelable.Should().BeFalse();
        dto.MotivoNoCancelable.Should().Be("Esta reserva está completada y ya no se puede cancelar.");
    }

    private static Reserva CrearReserva(DateTime inicio, Observaciones? observaciones)
    {
        Servicio servicio = Servicio.Crear(
            Guid.CreateVersion7(), Guid.CreateVersion7(), new NombreServicio("Corte"), new Duracion(45), new Precio(18.50m), Ahora);
        return Reserva.Crear(
            Guid.CreateVersion7(), servicio.EmpresaId, Guid.CreateVersion7(), Guid.CreateVersion7(), servicio, inicio,
            new NombrePersona("Lucia Perez"), new Telefono("611222333"), observaciones, Ahora.AddHours(-1));
    }
}
