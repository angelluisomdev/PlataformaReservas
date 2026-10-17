using FluentAssertions;
using PlataformaReservas.Aplicacion.Dtos.Empresas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Dtos.Empresas;

public sealed class PruebasFichaEmpresaDto
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ConvertirADto_copia_los_datos_publicos_ordena_por_nombre_y_une_el_horario()
    {
        Empresa empresa = Empresa.Crear(
            Guid.CreateVersion7(), new NombreEmpresa("Barberia Centro"), Slug.Desde("barberia-centro"), Guid.CreateVersion7(),
            new DescripcionEmpresa("<script>alert(1)</script>"), new Email("contacto@prueba.es"), new Telefono("600000000"),
            new Direccion("Calle Mayor 1"), new CodigoPostal("29001"), new Ciudad("Málaga"), Ahora);
        Servicio tinte = Servicio.Crear(Guid.CreateVersion7(), empresa.Id, new NombreServicio("Tinte"), new Duracion(60), new Precio(30m), Ahora);
        Servicio corte = Servicio.Crear(Guid.CreateVersion7(), empresa.Id, new NombreServicio("Corte"), new Duracion(30), new Precio(12m), Ahora);
        Profesional luis = ConHorario("Luis Perez", empresa.Id, 13, 18);
        Profesional ana = ConHorario("Ana Garcia", empresa.Id, 9, 14);

        FichaEmpresaDto ficha = FichaEmpresaDto.ConvertirADto(empresa, "Barbería", [tinte, corte], [luis, ana]);

        ficha.Nombre.Should().Be("Barberia Centro");
        ficha.Slug.Should().Be("barberia-centro");
        ficha.Categoria.Should().Be("Barbería");
        ficha.Descripcion.Should().Be("<script>alert(1)</script>");
        ficha.Direccion.Should().Be("Calle Mayor 1");
        ficha.CodigoPostal.Should().Be("29001");
        ficha.Ciudad.Should().Be("Málaga");
        ficha.Telefono.Should().Be("600000000");
        ficha.Servicios.Select(s => s.Nombre).Should().Equal("Corte", "Tinte");
        ficha.Profesionales.Select(p => p.Nombre).Should().Equal("Ana Garcia", "Luis Perez");
        ficha.Horario.Single(d => d.Dia == DayOfWeek.Monday).Intervalos
            .Should().Equal(new IntervaloHorario(new TimeOnly(9, 0), new TimeOnly(18, 0)));
    }

    private static Profesional ConHorario(string nombre, Guid empresaId, int inicio, int fin)
    {
        Profesional profesional = Profesional.Crear(Guid.CreateVersion7(), empresaId, new NombrePersona(nombre), Ahora);
        profesional.AgregarIntervalo(
            Guid.CreateVersion7(), DayOfWeek.Monday, new IntervaloHorario(new TimeOnly(inicio, 0), new TimeOnly(fin, 0)));
        return profesional;
    }
}
