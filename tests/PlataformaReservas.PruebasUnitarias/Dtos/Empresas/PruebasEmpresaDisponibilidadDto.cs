using FluentAssertions;
using PlataformaReservas.Aplicacion.Dtos.Empresas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Dtos.Empresas;

public sealed class PruebasEmpresaDisponibilidadDto
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ConvertirADto_copia_la_actividad_y_las_politicas_de_la_empresa()
    {
        Empresa empresa = NuevaEmpresa();
        empresa.CambiarPoliticas(new PoliticasReserva(30, 120, 90), Ahora);

        EmpresaDisponibilidadDto dto = EmpresaDisponibilidadDto.ConvertirADto(empresa);

        dto.Should().Be(new EmpresaDisponibilidadDto(true, 30, 120, 90));
    }

    [Fact]
    public void ConvertirADto_refleja_una_empresa_dada_de_baja()
    {
        Empresa empresa = NuevaEmpresa();
        empresa.DarDeBaja(Ahora);

        EmpresaDisponibilidadDto dto = EmpresaDisponibilidadDto.ConvertirADto(empresa);

        dto.Activa.Should().BeFalse();
    }

    private static Empresa NuevaEmpresa()
    {
        return Empresa.Crear(
            Guid.CreateVersion7(), new NombreEmpresa("Peluqueria Prueba"), Slug.Desde("peluqueria-prueba"), Guid.CreateVersion7(),
            null, new Email("contacto@prueba.es"), new Telefono("600000000"), new Direccion("Calle Mayor 1"),
            new CodigoPostal("28001"), new Ciudad("Madrid"), Ahora);
    }
}
