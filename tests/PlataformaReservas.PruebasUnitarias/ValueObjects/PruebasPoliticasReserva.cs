using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.ValueObjects;

public sealed class PruebasPoliticasReserva
{
    [Theory]
    [InlineData(1, 0, 1)]
    [InlineData(480, 43_200, 365)]
    public void Admite_los_limites_de_cada_rango(int intervalo, int antelacionMinima, int antelacionMaxima)
    {
        Action crear = () => _ = new PoliticasReserva(intervalo, antelacionMinima, antelacionMaxima);

        crear.Should().NotThrow();
    }

    [Theory]
    [InlineData(0, 60, 60)]
    [InlineData(481, 60, 60)]
    [InlineData(15, -1, 60)]
    [InlineData(15, 43_201, 60)]
    [InlineData(15, 60, 0)]
    [InlineData(15, 60, 366)]
    public void Rechaza_valores_fuera_de_rango(int intervalo, int antelacionMinima, int antelacionMaxima)
    {
        Action crear = () => _ = new PoliticasReserva(intervalo, antelacionMinima, antelacionMaxima);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Resolver_hereda_las_politicas_de_la_empresa_si_el_servicio_no_las_sobrescribe()
    {
        DateTime ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
        Empresa empresa = Empresa.Crear(
            Guid.CreateVersion7(), new NombreEmpresa("Peluqueria Prueba"), Slug.Desde("peluqueria-prueba"), Guid.CreateVersion7(),
            null, new Email("contacto@prueba.es"), new Telefono("600000000"), new Direccion("Calle Mayor 1"), new CodigoPostal("28001"), new Ciudad("Madrid"), ahora);
        Servicio servicio = Servicio.Crear(Guid.CreateVersion7(), empresa.Id, new NombreServicio("Corte"), new Duracion(45), new Precio(18.50m), ahora);

        PoliticasReserva efectivas = PoliticasReserva.Resolver(empresa, servicio);

        efectivas.Should().Be(empresa.Politicas);
    }

    [Fact]
    public void Dos_politicas_con_los_mismos_valores_son_iguales()
    {
        new PoliticasReserva(15, 60, 60).Should().Be(new PoliticasReserva(15, 60, 60));
    }
}
