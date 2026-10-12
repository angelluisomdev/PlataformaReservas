using FluentAssertions;
using PlataformaReservas.Aplicacion.Dtos.Reservas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Dtos.Reservas;

public sealed class PruebasReservaOcupadaDto
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ConvertirADto_copia_el_profesional_y_la_franja_en_utc()
    {
        Guid empresaId = Guid.CreateVersion7();
        Guid profesionalId = Guid.CreateVersion7();
        Servicio servicio = Servicio.Crear(Guid.CreateVersion7(), empresaId, new NombreServicio("Corte"), new Duracion(45), new Precio(18.50m), Ahora);
        DateTime inicio = Ahora.AddDays(1);
        Reserva reserva = Reserva.Crear(
            Guid.CreateVersion7(), empresaId, Guid.CreateVersion7(), profesionalId, servicio, inicio,
            new NombrePersona("Lucia Perez"), new Telefono("611222333"), null, Ahora);

        ReservaOcupadaDto dto = ReservaOcupadaDto.ConvertirADto(reserva);

        dto.Should().Be(new ReservaOcupadaDto(profesionalId, inicio, inicio.AddMinutes(45)));
        dto.InicioUtc.Kind.Should().Be(DateTimeKind.Utc);
    }
}
