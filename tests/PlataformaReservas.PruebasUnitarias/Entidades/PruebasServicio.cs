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

    [Fact]
    public void ActualizarDatos_cambia_los_datos_y_la_fecha_de_modificacion()
    {
        Servicio servicio = CrearServicio();

        servicio.ActualizarDatos(
            new NombreServicio("Corte y barba"), new DescripcionServicio("Con lavado"), new Duracion(60), new Precio(25m),
            Ahora.AddHours(1));

        servicio.Nombre.Should().Be(new NombreServicio("Corte y barba"));
        servicio.Descripcion.Should().Be(new DescripcionServicio("Con lavado"));
        servicio.Duracion.Minutos.Should().Be(60);
        servicio.Precio.Importe.Should().Be(25m);
        servicio.FechaModificacion.Should().Be(Ahora.AddHours(1));
    }

    [Fact]
    public void EstablecerPoliticas_guarda_cada_politica_por_separado_y_nula_es_heredar()
    {
        Servicio servicio = CrearServicio();

        servicio.EstablecerPoliticas(new IntervaloHuecos(30), null, new AntelacionMaximaReserva(90), Ahora);

        servicio.IntervaloHuecosMinutos.Should().Be(30);
        servicio.AntelacionMinimaMinutos.Should().BeNull();
        servicio.AntelacionMaximaDias.Should().Be(90);
    }

    [Fact]
    public void EstablecerPoliticas_sin_valores_vuelve_a_heredar_todas()
    {
        Servicio servicio = CrearServicio();
        servicio.EstablecerPoliticas(new IntervaloHuecos(30), new AntelacionMinimaReserva(0), new AntelacionMaximaReserva(90), Ahora);

        servicio.EstablecerPoliticas(null, null, null, Ahora);

        servicio.IntervaloHuecosMinutos.Should().BeNull();
        servicio.AntelacionMinimaMinutos.Should().BeNull();
        servicio.AntelacionMaximaDias.Should().BeNull();
    }

    [Fact]
    public void Desactivar_lo_desactiva_y_no_admite_una_segunda_vez()
    {
        Servicio servicio = CrearServicio();

        servicio.Desactivar(Ahora.AddHours(1));
        Action otraVez = () => servicio.Desactivar(Ahora.AddHours(2));

        servicio.Activo.Should().BeFalse();
        servicio.FechaModificacion.Should().Be(Ahora.AddHours(1));
        otraVez.Should().Throw<DominioException>();
    }

    [Fact]
    public void Los_cambios_rechazan_una_fecha_que_no_es_utc()
    {
        Servicio servicio = CrearServicio();
        DateTime local = DateTime.SpecifyKind(Ahora, DateTimeKind.Local);

        Action desactivar = () => servicio.Desactivar(local);
        Action politicas = () => servicio.EstablecerPoliticas(null, null, null, local);

        desactivar.Should().Throw<DominioException>();
        politicas.Should().Throw<DominioException>();
        servicio.Activo.Should().BeTrue();
    }
}
