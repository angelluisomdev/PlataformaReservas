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

    [Fact]
    public void ActualizarDatos_cambia_nombre_correo_y_telefono()
    {
        Profesional profesional = CrearProfesional();

        profesional.ActualizarDatos(new NombrePersona("Ana Lopez"), new Email("ana@prueba.es"), new Telefono("600111222"));

        profesional.NombreCompleto.Should().Be(new NombrePersona("Ana Lopez"));
        profesional.Email.Should().Be(new Email("ana@prueba.es"));
        profesional.Telefono.Should().Be(new Telefono("600111222"));
    }

    [Fact]
    public void ActualizarDatos_admite_quitar_correo_y_telefono()
    {
        Profesional profesional = CrearProfesional();
        profesional.ActualizarDatos(new NombrePersona("Ana Lopez"), new Email("ana@prueba.es"), new Telefono("600111222"));

        profesional.ActualizarDatos(new NombrePersona("Ana Lopez"), null, null);

        profesional.Email.Should().BeNull();
        profesional.Telefono.Should().BeNull();
    }

    [Fact]
    public void Desactivar_lo_desactiva_y_no_admite_una_segunda_vez()
    {
        Profesional profesional = CrearProfesional();

        profesional.Desactivar();
        Action otraVez = profesional.Desactivar;

        profesional.Activo.Should().BeFalse();
        otraVez.Should().Throw<DominioException>();
    }

    [Fact]
    public void AsociarServicio_lo_anade_a_los_que_presta()
    {
        Profesional profesional = CrearProfesional();
        Guid servicioId = Guid.CreateVersion7();

        profesional.AsociarServicio(servicioId);

        profesional.Presta(servicioId).Should().BeTrue();
        profesional.ServiciosQuePresta.Should().ContainSingle().Which.Should().Be(servicioId);
    }

    [Fact]
    public void AsociarServicio_rechaza_el_mismo_servicio_dos_veces_y_un_identificador_vacio()
    {
        Profesional profesional = CrearProfesional();
        Guid servicioId = Guid.CreateVersion7();
        profesional.AsociarServicio(servicioId);

        Action duplicado = () => profesional.AsociarServicio(servicioId);
        Action vacio = () => profesional.AsociarServicio(Guid.Empty);

        duplicado.Should().Throw<DominioException>();
        vacio.Should().Throw<DominioException>();
        profesional.ServiciosQuePresta.Should().ContainSingle();
    }

    [Fact]
    public void DesasociarServicio_lo_quita_y_rechaza_un_servicio_que_no_presta()
    {
        Profesional profesional = CrearProfesional();
        Guid servicioId = Guid.CreateVersion7();
        profesional.AsociarServicio(servicioId);

        profesional.DesasociarServicio(servicioId);
        Action otraVez = () => profesional.DesasociarServicio(servicioId);

        profesional.Presta(servicioId).Should().BeFalse();
        otraVez.Should().Throw<DominioException>();
    }
}
