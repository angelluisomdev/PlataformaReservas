using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Entidades;

public sealed class PruebasServicio
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    private static Servicio CrearServicio(
        Guid? id = null,
        Guid? empresaId = null,
        DateTime? ahoraUtc = null)
    {
        return Servicio.Crear(
            id ?? Guid.CreateVersion7(),
            empresaId ?? Guid.CreateVersion7(),
            new NombreServicio("Corte"),
            new Duracion(45),
            new Precio(18.50m),
            ahoraUtc ?? Ahora);
    }

    [Fact]
    public void Nace_activo_con_su_nombre_duracion_y_precio_y_sin_politicas_propias()
    {
        Servicio servicio = CrearServicio();

        servicio.Activo.Should().BeTrue();
        servicio.Nombre.Should().Be(new NombreServicio("Corte"));
        servicio.Duracion.Minutos.Should().Be(45);
        servicio.Precio.Importe.Should().Be(18.50m);
        servicio.IntervaloHuecosMinutos.Should().BeNull();
        servicio.AntelacionMinimaMinutos.Should().BeNull();
        servicio.AntelacionMaximaDias.Should().BeNull();
    }

    [Fact]
    public void Las_fechas_de_alta_y_modificacion_son_el_instante_de_creacion()
    {
        Servicio servicio = CrearServicio();

        servicio.FechaAlta.Should().Be(Ahora);
        servicio.FechaModificacion.Should().Be(Ahora);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("empresa")]
    public void Rechaza_identificadores_vacios(string cual)
    {
        Action crear = cual == "id"
            ? () => CrearServicio(id: Guid.Empty)
            : () => CrearServicio(empresaId: Guid.Empty);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Rechaza_una_fecha_de_alta_que_no_es_utc()
    {
        Action crear = () => CrearServicio(ahoraUtc: DateTime.SpecifyKind(Ahora, DateTimeKind.Unspecified));

        crear.Should().Throw<DominioException>();
    }
}
