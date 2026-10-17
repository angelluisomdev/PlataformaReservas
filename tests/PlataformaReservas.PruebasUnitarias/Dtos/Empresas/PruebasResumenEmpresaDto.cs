using FluentAssertions;
using PlataformaReservas.Aplicacion.Dtos.Empresas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Dtos.Empresas;

public sealed class PruebasResumenEmpresaDto
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ConvertirADto_copia_nombre_slug_categoria_ciudad_y_descripcion()
    {
        Empresa empresa = NuevaEmpresa(new DescripcionEmpresa("Cortes clasicos"));

        ResumenEmpresaDto dto = ResumenEmpresaDto.ConvertirADto(empresa, "Barbería");

        dto.Should().Be(new ResumenEmpresaDto("Barberia Centro", "barberia-centro", "Barbería", "Málaga", "Cortes clasicos"));
    }

    [Fact]
    public void Una_descripcion_larga_se_corta_a_la_longitud_breve()
    {
        Empresa empresa = NuevaEmpresa(new DescripcionEmpresa(new string('a', 300)));

        ResumenEmpresaDto dto = ResumenEmpresaDto.ConvertirADto(empresa, "Barbería");

        dto.DescripcionBreve.Should().HaveLength(ResumenEmpresaDto.LongitudDescripcionBreve);
    }

    [Fact]
    public void Una_descripcion_de_justo_la_longitud_breve_no_se_corta()
    {
        string texto = new('b', ResumenEmpresaDto.LongitudDescripcionBreve);
        Empresa empresa = NuevaEmpresa(new DescripcionEmpresa(texto));

        ResumenEmpresaDto.ConvertirADto(empresa, "Barbería").DescripcionBreve.Should().Be(texto);
    }

    [Fact]
    public void Sin_descripcion_la_breve_es_nula()
    {
        ResumenEmpresaDto.ConvertirADto(NuevaEmpresa(null), "Barbería").DescripcionBreve.Should().BeNull();
    }

    private static Empresa NuevaEmpresa(DescripcionEmpresa? descripcion)
    {
        return Empresa.Crear(
            Guid.CreateVersion7(), new NombreEmpresa("Barberia Centro"), Slug.Desde("barberia-centro"), Guid.CreateVersion7(),
            descripcion, new Email("contacto@prueba.es"), new Telefono("600000000"), new Direccion("Calle Mayor 1"),
            new CodigoPostal("29001"), new Ciudad("Málaga"), Ahora);
    }
}
