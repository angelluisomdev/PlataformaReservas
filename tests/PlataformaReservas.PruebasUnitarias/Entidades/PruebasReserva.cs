using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Entidades;

public sealed class PruebasReserva
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly Guid EmpresaId = Guid.CreateVersion7();
    private static readonly NombrePersona Cliente = new("Lucia Perez");
    private static readonly Telefono TelefonoCliente = new("611222333");

    private static Servicio ServicioDe(Guid empresaId)
    {
        return Servicio.Crear(
            Guid.CreateVersion7(), empresaId, new NombreServicio("Corte"), new Duracion(45), new Precio(18.50m), Ahora);
    }

    private static Reserva CrearReserva(
        Servicio servicio,
        Observaciones? observaciones = null,
        Guid? id = null,
        Guid? usuarioId = null,
        Guid? profesionalId = null,
        DateTime? inicioUtc = null,
        DateTime? ahoraUtc = null)
    {
        return Reserva.Crear(
            id ?? Guid.CreateVersion7(), EmpresaId, usuarioId ?? Guid.CreateVersion7(),
            profesionalId ?? Guid.CreateVersion7(), servicio, inicioUtc ?? Ahora.AddDays(1),
            Cliente, TelefonoCliente, observaciones, ahoraUtc ?? Ahora);
    }

    [Fact]
    public void Nace_confirmada()
    {
        Reserva reserva = CrearReserva(ServicioDe(EmpresaId));

        reserva.Estado.Should().Be(EstadoReserva.Confirmada);
        reserva.FechaCancelacion.Should().BeNull();
    }

    [Fact]
    public void Calcula_el_fin_y_copia_precio_y_duracion_del_servicio()
    {
        Servicio servicio = ServicioDe(EmpresaId);

        Reserva reserva = CrearReserva(servicio);

        reserva.Franja.FinUtc.Should().Be(reserva.Franja.InicioUtc.AddMinutes(45));
        reserva.DuracionAplicada.Minutos.Should().Be(45);
        reserva.PrecioAplicado.Importe.Should().Be(18.50m);
        reserva.ServicioId.Should().Be(servicio.Id);
    }

    [Fact]
    public void Conserva_los_datos_del_cliente()
    {
        Observaciones observaciones = new("Llegare cinco minutos tarde");

        Reserva reserva = CrearReserva(ServicioDe(EmpresaId), observaciones);

        reserva.NombreCliente.Should().Be(Cliente);
        reserva.TelefonoCliente.Should().Be(TelefonoCliente);
        reserva.Observaciones.Should().Be(observaciones);
    }

    [Fact]
    public void Admite_observaciones_nulas()
    {
        CrearReserva(ServicioDe(EmpresaId)).Observaciones.Should().BeNull();
    }

    [Fact]
    public void Las_fechas_de_alta_y_modificacion_son_el_instante_de_creacion()
    {
        Reserva reserva = CrearReserva(ServicioDe(EmpresaId));

        reserva.FechaAlta.Should().Be(Ahora);
        reserva.FechaModificacion.Should().Be(Ahora);
    }

    [Fact]
    public void Rechaza_un_servicio_de_otra_empresa()
    {
        Action crear = () => CrearReserva(ServicioDe(Guid.CreateVersion7()));

        crear.Should().Throw<DominioException>();
    }

    [Theory]
    [InlineData("id")]
    [InlineData("usuario")]
    [InlineData("profesional")]
    public void Rechaza_identificadores_vacios(string cual)
    {
        Servicio servicio = ServicioDe(EmpresaId);

        Action crear = cual switch
        {
            "id" => () => CrearReserva(servicio, id: Guid.Empty),
            "usuario" => () => CrearReserva(servicio, usuarioId: Guid.Empty),
            _ => () => CrearReserva(servicio, profesionalId: Guid.Empty),
        };

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Rechaza_un_inicio_que_no_es_utc()
    {
        Action crear = () => CrearReserva(
            ServicioDe(EmpresaId), inicioUtc: DateTime.SpecifyKind(Ahora.AddDays(1), DateTimeKind.Local));

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Rechaza_una_fecha_de_alta_que_no_es_utc()
    {
        Action crear = () => CrearReserva(ServicioDe(EmpresaId), ahoraUtc: DateTime.SpecifyKind(Ahora, DateTimeKind.Local));

        crear.Should().Throw<DominioException>();
    }
}
