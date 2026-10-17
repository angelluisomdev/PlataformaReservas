using FluentAssertions;
using PlataformaReservas.Aplicacion.Dtos.Empresas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Dtos.Empresas;

public sealed class PruebasServicioFichaDto
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ConvertirADto_copia_nombre_duracion_y_precio_sin_las_politicas()
    {
        Servicio servicio = Servicio.Crear(
            Guid.CreateVersion7(), Guid.CreateVersion7(), new NombreServicio("Corte"), new Duracion(30), new Precio(12.5m), Ahora);

        ServicioFichaDto dto = ServicioFichaDto.ConvertirADto(servicio);

        dto.Should().Be(new ServicioFichaDto("Corte", null, 30, 12.5m));
    }
}
