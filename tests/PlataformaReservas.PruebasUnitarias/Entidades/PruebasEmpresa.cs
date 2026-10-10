using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Entidades;

public sealed class PruebasEmpresa
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    private static Empresa CrearEmpresa(
        DescripcionEmpresa? descripcion = null,
        Guid? id = null,
        Guid? categoriaId = null,
        DateTime? ahoraUtc = null)
    {
        return Empresa.Crear(
            id ?? Guid.CreateVersion7(),
            new NombreEmpresa("Peluqueria Prueba"),
            Slug.Desde("peluqueria-prueba"),
            categoriaId ?? Guid.CreateVersion7(),
            descripcion,
            new Email("contacto@prueba.es"),
            new Telefono("600000000"),
            new Direccion("Calle Mayor 1"),
            new CodigoPostal("28001"),
            new Ciudad("Madrid"),
            ahoraUtc ?? Ahora);
    }

    [Fact]
    public void Nace_activa_en_madrid_con_las_politicas_por_defecto()
    {
        Empresa empresa = CrearEmpresa();

        empresa.Activa.Should().BeTrue();
        empresa.ZonaHoraria.Should().Be("Europe/Madrid");
        empresa.Politicas.Should().Be(new PoliticasReserva(15, 60, 60));
        empresa.FechaAlta.Should().Be(Ahora);
        empresa.FechaModificacion.Should().Be(Ahora);
    }

    [Fact]
    public void Conserva_los_datos_recibidos()
    {
        Empresa empresa = CrearEmpresa(new DescripcionEmpresa("Cortes"));

        empresa.Nombre.Should().Be(new NombreEmpresa("Peluqueria Prueba"));
        empresa.Slug.Should().Be(Slug.Desde("peluqueria-prueba"));
        empresa.Descripcion.Should().Be(new DescripcionEmpresa("Cortes"));
        empresa.Email.Should().Be(new Email("contacto@prueba.es"));
        empresa.Telefono.Should().Be(new Telefono("600000000"));
        empresa.Direccion.Should().Be(new Direccion("Calle Mayor 1"));
        empresa.CodigoPostal.Should().Be(new CodigoPostal("28001"));
        empresa.Ciudad.Should().Be(new Ciudad("Madrid"));
    }

    [Fact]
    public void Admite_una_descripcion_nula()
    {
        CrearEmpresa(descripcion: null).Descripcion.Should().BeNull();
    }

    [Theory]
    [InlineData("id")]
    [InlineData("categoria")]
    public void Rechaza_identificadores_vacios(string cual)
    {
        Action crear = cual == "id"
            ? () => CrearEmpresa(id: Guid.Empty)
            : () => CrearEmpresa(categoriaId: Guid.Empty);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Rechaza_una_fecha_de_alta_que_no_es_utc()
    {
        Action crear = () => CrearEmpresa(ahoraUtc: DateTime.SpecifyKind(Ahora, DateTimeKind.Local));

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void ActualizarDatos_cambia_los_datos_y_la_fecha_de_modificacion_pero_no_el_slug()
    {
        Empresa empresa = CrearEmpresa();
        DateTime despues = Ahora.AddHours(1);

        empresa.ActualizarDatos(
            new NombreEmpresa("Barberia Nueva"), new DescripcionEmpresa("Cortes"), new Email("nueva@prueba.es"),
            new Telefono("611111111"), new Direccion("Calle Nueva 2"), new CodigoPostal("37001"), new Ciudad("Salamanca"),
            despues);

        empresa.Nombre.Should().Be(new NombreEmpresa("Barberia Nueva"));
        empresa.Descripcion.Should().Be(new DescripcionEmpresa("Cortes"));
        empresa.Ciudad.Should().Be(new Ciudad("Salamanca"));
        empresa.Slug.Should().Be(Slug.Desde("peluqueria-prueba"));
        empresa.FechaModificacion.Should().Be(despues);
    }

    [Fact]
    public void CambiarCategoria_cambia_la_categoria_y_rechaza_un_identificador_vacio()
    {
        Empresa empresa = CrearEmpresa();
        Guid categoria = Guid.CreateVersion7();

        empresa.CambiarCategoria(categoria, Ahora);
        Action vacia = () => empresa.CambiarCategoria(Guid.Empty, Ahora);

        empresa.CategoriaId.Should().Be(categoria);
        vacia.Should().Throw<DominioException>();
    }

    [Fact]
    public void CambiarPoliticas_sustituye_las_politicas()
    {
        Empresa empresa = CrearEmpresa();

        empresa.CambiarPoliticas(new PoliticasReserva(30, 0, 90), Ahora.AddHours(1));

        empresa.Politicas.Should().Be(new PoliticasReserva(30, 0, 90));
        empresa.FechaModificacion.Should().Be(Ahora.AddHours(1));
    }

    [Fact]
    public void DarDeBaja_la_desactiva_y_no_admite_una_segunda_baja()
    {
        Empresa empresa = CrearEmpresa();

        empresa.DarDeBaja(Ahora.AddHours(1));
        Action otraVez = () => empresa.DarDeBaja(Ahora.AddHours(2));

        empresa.Activa.Should().BeFalse();
        empresa.FechaModificacion.Should().Be(Ahora.AddHours(1));
        otraVez.Should().Throw<DominioException>();
    }

    [Fact]
    public void Reactivar_la_activa_y_no_admite_reactivar_una_activa()
    {
        Empresa empresa = CrearEmpresa();
        Action activa = () => empresa.Reactivar(Ahora);
        empresa.DarDeBaja(Ahora);

        empresa.Reactivar(Ahora.AddHours(1));

        empresa.Activa.Should().BeTrue();
        activa.Should().Throw<DominioException>();
    }

    [Fact]
    public void Los_cambios_rechazan_una_fecha_que_no_es_utc()
    {
        Empresa empresa = CrearEmpresa();
        DateTime local = DateTime.SpecifyKind(Ahora, DateTimeKind.Local);

        Action baja = () => empresa.DarDeBaja(local);
        Action politicas = () => empresa.CambiarPoliticas(new PoliticasReserva(15, 60, 60), local);

        baja.Should().Throw<DominioException>();
        politicas.Should().Throw<DominioException>();
        empresa.Activa.Should().BeTrue();
    }
}
