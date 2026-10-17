using FluentAssertions;
using PlataformaReservas.Aplicacion.Dtos.Profesionales;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Dtos.Profesionales;

public sealed class PruebasProfesionalReservaDto
{
    [Fact]
    public void ConvertirADto_solo_copia_el_identificador_y_el_nombre()
    {
        Profesional profesional = Profesional.Crear(
            Guid.CreateVersion7(), Guid.CreateVersion7(), new NombrePersona("Ana Garcia"), new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc));
        profesional.ActualizarDatos(new NombrePersona("Ana Garcia"), new Email("ana@prueba.es"), new Telefono("600111222"));

        ProfesionalReservaDto dto = ProfesionalReservaDto.ConvertirADto(profesional);

        dto.Should().Be(new ProfesionalReservaDto(profesional.Id, "Ana Garcia"));
    }
}
