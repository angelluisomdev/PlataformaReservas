using FluentAssertions;
using PlataformaReservas.Aplicacion.Dtos.Empresas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Dtos.Empresas;

public sealed class PruebasDatosReservaDto
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ConvertirADto_ordena_los_servicios_y_resuelve_la_ventana_efectiva_de_cada_uno()
    {
        Empresa empresa = Empresa.Crear(
            Guid.CreateVersion7(), new NombreEmpresa("Barberia Centro"), Slug.Desde("barberia-centro"), Guid.CreateVersion7(),
            null, new Email("contacto@prueba.es"), new Telefono("600000000"),
            new Direccion("Calle Mayor 1"), new CodigoPostal("29001"), new Ciudad("Málaga"), Ahora);
        empresa.CambiarPoliticas(new PoliticasReserva(15, 60, 30), Ahora);
        Servicio tinte = Servicio.Crear(Guid.CreateVersion7(), empresa.Id, new NombreServicio("Tinte"), new Duracion(60), new Precio(30m), Ahora);
        tinte.EstablecerPoliticas(null, null, new AntelacionMaximaReserva(7), Ahora);
        Servicio corte = Servicio.Crear(Guid.CreateVersion7(), empresa.Id, new NombreServicio("Corte"), new Duracion(30), new Precio(12m), Ahora);
        Profesional ana = Profesional.Crear(Guid.CreateVersion7(), empresa.Id, new NombrePersona("Ana Garcia"), Ahora);
        ana.AsociarServicio(corte.Id);

        DatosReservaDto datos = DatosReservaDto.ConvertirADto(empresa, [tinte, corte], [ana]);

        datos.EmpresaId.Should().Be(empresa.Id);
        datos.Nombre.Should().Be("Barberia Centro");
        datos.Slug.Should().Be("barberia-centro");
        datos.Servicios.Select(s => s.Nombre).Should().Equal("Corte", "Tinte");
        datos.Servicios[0].AntelacionMaximaDias.Should().Be(30);
        datos.Servicios[1].AntelacionMaximaDias.Should().Be(7);
        datos.Servicios[0].Profesionales.Should().ContainSingle().Which.Id.Should().Be(ana.Id);
        datos.Servicios[1].Profesionales.Should().BeEmpty();
    }
}
