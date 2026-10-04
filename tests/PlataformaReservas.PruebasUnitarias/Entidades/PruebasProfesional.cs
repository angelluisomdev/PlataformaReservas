using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Entidades;

public sealed class PruebasProfesional
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    private static Profesional CrearProfesional(
        Guid? id = null,
        Guid? empresaId = null,
        DateTime? ahoraUtc = null)
    {
        return Profesional.Crear(
            id ?? Guid.CreateVersion7(), empresaId ?? Guid.CreateVersion7(), new NombrePersona("Ana Garcia"), ahoraUtc ?? Ahora);
    }

    [Fact]
    public void Nace_activo_sin_horarios_excepciones_ni_servicios()
    {
        Profesional profesional = CrearProfesional();

        profesional.Activo.Should().BeTrue();
        profesional.NombreCompleto.Should().Be(new NombrePersona("Ana Garcia"));
        profesional.Email.Should().BeNull();
        profesional.Telefono.Should().BeNull();
        profesional.FechaAlta.Should().Be(Ahora);
        profesional.Horarios.Should().BeEmpty();
        profesional.Excepciones.Should().BeEmpty();
        profesional.ServiciosQuePresta.Should().BeEmpty();
    }

    [Theory]
    [InlineData("id")]
    [InlineData("empresa")]
    public void Rechaza_identificadores_vacios(string cual)
    {
        Action crear = cual == "id"
            ? () => CrearProfesional(id: Guid.Empty)
            : () => CrearProfesional(empresaId: Guid.Empty);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Rechaza_una_fecha_de_alta_que_no_es_utc()
    {
        Action crear = () => CrearProfesional(ahoraUtc: DateTime.SpecifyKind(Ahora, DateTimeKind.Local));

        crear.Should().Throw<DominioException>();
    }
}
