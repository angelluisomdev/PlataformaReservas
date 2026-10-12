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

    [Fact]
    public void CancelarPorEmpresa_cancela_una_confirmada_aunque_ya_haya_empezado()
    {
        Reserva reserva = CrearReserva(ServicioDe(EmpresaId), inicioUtc: Ahora.AddHours(-1));
        DateTime cancelacion = Ahora.AddMinutes(5);

        reserva.CancelarPorEmpresa(cancelacion);

        reserva.Estado.Should().Be(EstadoReserva.Cancelada);
        reserva.FechaCancelacion.Should().Be(cancelacion);
        reserva.FechaModificacion.Should().Be(cancelacion);
    }

    [Fact]
    public void CancelarPorEmpresa_rechaza_una_reserva_ya_cancelada()
    {
        Reserva reserva = CrearReserva(ServicioDe(EmpresaId));
        reserva.CancelarPorEmpresa(Ahora);

        Action otraVez = () => reserva.CancelarPorEmpresa(Ahora.AddMinutes(1));

        otraVez.Should().Throw<DominioException>();
        reserva.FechaCancelacion.Should().Be(Ahora);
    }

    [Fact]
    public void CancelarPorEmpresa_rechaza_una_fecha_que_no_es_utc()
    {
        Reserva reserva = CrearReserva(ServicioDe(EmpresaId));

        Action cancelar = () => reserva.CancelarPorEmpresa(DateTime.SpecifyKind(Ahora, DateTimeKind.Local));

        cancelar.Should().Throw<DominioException>();
        reserva.Estado.Should().Be(EstadoReserva.Confirmada);
    }

    [Fact]
    public void CancelarPorEmpresa_rechaza_una_reserva_completada()
    {
        Reserva reserva = CrearReserva(ServicioDe(EmpresaId), inicioUtc: Ahora.AddHours(-2));
        reserva.Completar(Ahora);

        Action cancelar = () => reserva.CancelarPorEmpresa(Ahora.AddMinutes(1));

        cancelar.Should().Throw<DominioException>();
        reserva.Estado.Should().Be(EstadoReserva.Completada);
    }

    [Fact]
    public void Crear_con_los_datos_del_servicio_calcula_el_fin_y_copia_precio_y_duracion()
    {
        Guid servicioId = Guid.CreateVersion7();
        DateTime inicio = Ahora.AddDays(1);

        Reserva reserva = Reserva.Crear(
            Guid.CreateVersion7(), EmpresaId, Guid.CreateVersion7(), Guid.CreateVersion7(), servicioId,
            new Duracion(30), new Precio(12m), inicio, Cliente, TelefonoCliente, null, Ahora);

        reserva.Franja.InicioUtc.Should().Be(inicio);
        reserva.Franja.FinUtc.Should().Be(inicio.AddMinutes(30));
        reserva.DuracionAplicada.Minutos.Should().Be(30);
        reserva.PrecioAplicado.Importe.Should().Be(12m);
        reserva.ServicioId.Should().Be(servicioId);
        reserva.Estado.Should().Be(EstadoReserva.Confirmada);
        reserva.FechaAlta.Should().Be(Ahora);
    }

    [Fact]
    public void Crear_con_los_datos_del_servicio_rechaza_un_servicio_vacio()
    {
        Action crear = () => Reserva.Crear(
            Guid.CreateVersion7(), EmpresaId, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.Empty,
            new Duracion(30), new Precio(12m), Ahora.AddDays(1), Cliente, TelefonoCliente, null, Ahora);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void CancelarPorCliente_cancela_una_confirmada_futura()
    {
        Reserva reserva = CrearReserva(ServicioDe(EmpresaId), inicioUtc: Ahora.AddHours(3));

        reserva.CancelarPorCliente(Ahora);

        reserva.Estado.Should().Be(EstadoReserva.Cancelada);
        reserva.FechaCancelacion.Should().Be(Ahora);
        reserva.FechaModificacion.Should().Be(Ahora);
    }

    [Fact]
    public void CancelarPorCliente_admite_el_instante_justo_anterior_al_inicio()
    {
        Reserva reserva = CrearReserva(ServicioDe(EmpresaId), inicioUtc: Ahora.AddTicks(1));

        reserva.CancelarPorCliente(Ahora);

        reserva.Estado.Should().Be(EstadoReserva.Cancelada);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    public void CancelarPorCliente_rechaza_una_reserva_que_ya_ha_empezado(int minutosDesdeAhora)
    {
        Reserva reserva = CrearReserva(ServicioDe(EmpresaId), inicioUtc: Ahora.AddMinutes(minutosDesdeAhora));

        Action cancelar = () => reserva.CancelarPorCliente(Ahora);

        cancelar.Should().Throw<DominioException>();
        reserva.Estado.Should().Be(EstadoReserva.Confirmada);
        reserva.FechaCancelacion.Should().BeNull();
    }

    [Fact]
    public void CancelarPorCliente_rechaza_una_reserva_cancelada_y_repetirla_lanza()
    {
        Reserva reserva = CrearReserva(ServicioDe(EmpresaId));
        reserva.CancelarPorCliente(Ahora);

        Action otraVez = () => reserva.CancelarPorCliente(Ahora.AddMinutes(1));

        otraVez.Should().Throw<DominioException>();
        reserva.FechaCancelacion.Should().Be(Ahora);
    }

    [Fact]
    public void CancelarPorCliente_rechaza_una_reserva_completada()
    {
        Reserva reserva = CrearReserva(ServicioDe(EmpresaId), inicioUtc: Ahora.AddHours(-2));
        reserva.Completar(Ahora);

        Action cancelar = () => reserva.CancelarPorCliente(Ahora);

        cancelar.Should().Throw<DominioException>();
        reserva.Estado.Should().Be(EstadoReserva.Completada);
    }

    [Fact]
    public void CancelarPorCliente_rechaza_una_fecha_que_no_es_utc()
    {
        Reserva reserva = CrearReserva(ServicioDe(EmpresaId));

        Action cancelar = () => reserva.CancelarPorCliente(DateTime.SpecifyKind(Ahora, DateTimeKind.Local));

        cancelar.Should().Throw<DominioException>();
        reserva.Estado.Should().Be(EstadoReserva.Confirmada);
    }

    [Fact]
    public void Completar_completa_una_confirmada_ya_terminada()
    {
        Reserva reserva = CrearReserva(ServicioDe(EmpresaId), inicioUtc: Ahora.AddMinutes(-45).AddTicks(-1));

        reserva.Completar(Ahora);

        reserva.Estado.Should().Be(EstadoReserva.Completada);
        reserva.FechaModificacion.Should().Be(Ahora);
        reserva.FechaCancelacion.Should().BeNull();
    }

    [Theory]
    [InlineData(-45)]
    [InlineData(-10)]
    [InlineData(60)]
    public void Completar_rechaza_una_reserva_que_no_ha_terminado(int minutosDesdeAhora)
    {
        Reserva reserva = CrearReserva(ServicioDe(EmpresaId), inicioUtc: Ahora.AddMinutes(minutosDesdeAhora));

        Action completar = () => reserva.Completar(Ahora);

        completar.Should().Throw<DominioException>();
        reserva.Estado.Should().Be(EstadoReserva.Confirmada);
    }

    [Fact]
    public void Completar_rechaza_una_reserva_cancelada()
    {
        Reserva reserva = CrearReserva(ServicioDe(EmpresaId), inicioUtc: Ahora.AddHours(-2));
        reserva.CancelarPorEmpresa(Ahora.AddHours(-3));

        Action completar = () => reserva.Completar(Ahora);

        completar.Should().Throw<DominioException>();
        reserva.Estado.Should().Be(EstadoReserva.Cancelada);
    }

    [Fact]
    public void Completar_repetido_lanza()
    {
        Reserva reserva = CrearReserva(ServicioDe(EmpresaId), inicioUtc: Ahora.AddHours(-2));
        reserva.Completar(Ahora);

        Action otraVez = () => reserva.Completar(Ahora.AddMinutes(1));

        otraVez.Should().Throw<DominioException>();
        reserva.FechaModificacion.Should().Be(Ahora);
    }

    [Fact]
    public void Completar_rechaza_una_fecha_que_no_es_utc()
    {
        Reserva reserva = CrearReserva(ServicioDe(EmpresaId), inicioUtc: Ahora.AddHours(-2));

        Action completar = () => reserva.Completar(DateTime.SpecifyKind(Ahora, DateTimeKind.Local));

        completar.Should().Throw<DominioException>();
        reserva.Estado.Should().Be(EstadoReserva.Confirmada);
    }
}
