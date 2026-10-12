using FluentAssertions;
using PlataformaReservas.Aplicacion.Dtos.Servicios;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Dtos.Servicios;

public sealed class PruebasServicioDisponibilidadDto
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ConvertirADto_sin_politicas_propias_deja_los_valores_opcionales_a_nulo()
    {
        Servicio servicio = NuevoServicio();

        ServicioDisponibilidadDto dto = ServicioDisponibilidadDto.ConvertirADto(servicio);

        dto.Should().Be(new ServicioDisponibilidadDto(true, 45, null, null, null));
    }

    [Fact]
    public void ConvertirADto_copia_las_politicas_que_sobrescribe_el_servicio()
    {
        Servicio servicio = NuevoServicio();
        servicio.EstablecerPoliticas(new IntervaloHuecos(20), new AntelacionMinimaReserva(0), new AntelacionMaximaReserva(7), Ahora);

        ServicioDisponibilidadDto dto = ServicioDisponibilidadDto.ConvertirADto(servicio);

        dto.Should().Be(new ServicioDisponibilidadDto(true, 45, 20, 0, 7));
    }

    [Fact]
    public void ConvertirADto_refleja_un_servicio_desactivado()
    {
        Servicio servicio = NuevoServicio();
        servicio.Desactivar(Ahora);

        ServicioDisponibilidadDto dto = ServicioDisponibilidadDto.ConvertirADto(servicio);

        dto.Activo.Should().BeFalse();
    }

    private static Servicio NuevoServicio()
    {
        return Servicio.Crear(Guid.CreateVersion7(), Guid.CreateVersion7(), new NombreServicio("Corte"), new Duracion(45), new Precio(18.50m), Ahora);
    }
}
