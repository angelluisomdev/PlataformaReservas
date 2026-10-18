using FluentAssertions;
using PlataformaReservas.Aplicacion.Dtos.Cuenta;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Dtos.Cuenta;

public sealed class PruebasPerfilDto
{
    [Fact]
    public void ConvertirADto_copia_el_nombre_y_el_telefono()
    {
        PerfilDto dto = PerfilDto.ConvertirADto(new NombrePersona("Ana Garcia"), new Telefono("611222333"));

        dto.Should().Be(new PerfilDto("Ana Garcia", "611222333"));
    }

    [Fact]
    public void ConvertirADto_sin_telefono_lo_deja_nulo()
    {
        PerfilDto dto = PerfilDto.ConvertirADto(new NombrePersona("Ana Garcia"), null);

        dto.Telefono.Should().BeNull();
    }
}
