using FluentAssertions;
using PlataformaReservas.Aplicacion.Dtos.Empresas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Dtos.Empresas;

public sealed class PruebasProfesionalFichaDto
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ConvertirADto_solo_expone_el_nombre_y_nunca_el_email_ni_el_telefono()
    {
        Profesional profesional = Profesional.Crear(Guid.CreateVersion7(), Guid.CreateVersion7(), new NombrePersona("Ana Garcia"), Ahora);
        profesional.ActualizarDatos(new NombrePersona("Ana Garcia"), new Email("ana@prueba.es"), new Telefono("600111222"));

        ProfesionalFichaDto dto = ProfesionalFichaDto.ConvertirADto(profesional);

        dto.Should().Be(new ProfesionalFichaDto("Ana Garcia"));
        typeof(ProfesionalFichaDto).GetProperties().Select(p => p.Name).Should().Equal(nameof(ProfesionalFichaDto.Nombre));
    }
}
