using FluentAssertions;
using PlataformaReservas.Aplicacion.Dtos.Notificaciones;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Dtos.Notificaciones;

public sealed class PruebasDatosReservaNotificacionDto
{
    [Fact]
    public void ConvertirADto_copia_los_valores_y_une_la_direccion_completa()
    {
        Guid id = Guid.CreateVersion7();
        DateTime inicio = new(2026, 10, 5, 7, 30, 0, DateTimeKind.Utc);

        DatosReservaNotificacionDto dto = DatosReservaNotificacionDto.ConvertirADto(
            id, new NombreEmpresa("Barberia Centro"), new Direccion("Calle Mayor 1"), new CodigoPostal("37001"), new Ciudad("Salamanca"),
            new Email("contacto@barberia.es"), new NombreServicio("Corte"), new NombrePersona("Ana Garcia"),
            new FranjaHoraria(inicio, inicio.AddMinutes(45)), new Duracion(45), new Precio(18.50m), new NombrePersona("Lucia Perez"),
            new Telefono("611222333"), new Email("lucia@ejemplo.es"));

        dto.Should().Be(new DatosReservaNotificacionDto(
            id, "Barberia Centro", "Calle Mayor 1, 37001 Salamanca", "contacto@barberia.es", "Corte", "Ana Garcia", inicio, 45, 18.50m,
            "Lucia Perez", "611222333", "lucia@ejemplo.es"));
    }
}
