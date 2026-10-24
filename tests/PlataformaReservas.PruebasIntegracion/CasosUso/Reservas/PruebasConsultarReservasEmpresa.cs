using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.CasosUso.Reservas;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos.Reservas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Reservas;

[Collection("BaseDatos")]
public sealed class PruebasConsultarReservasEmpresa(BaseDatosFixture baseDatos)
{
    private static readonly DateOnly Hoy = new(2026, 10, 3);

    [Fact]
    public async Task Devuelve_las_reservas_del_dia_local_pedido_con_el_reloj_del_sistema()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva terminada = escenario.Reserva(DominioPrueba.Ahora.AddHours(-2));
        Reserva futura = escenario.Reserva(DominioPrueba.Ahora.AddHours(2));
        Reserva manana = escenario.Reserva(DominioPrueba.Ahora.AddDays(1));
        await EscenarioEmpresa.GuardarAsync(baseDatos, terminada, futura, manana);

        Resultado<IReadOnlyList<ReservaEmpresaDto>> resultado = await ConsultarAsync(
            escenario.Empresa.Id, new ConsultarReservasEmpresaComando(Hoy, null, null));

        resultado.EsExito.Should().BeTrue();
        resultado.Valor!.Select(r => r.Id).Should().Equal(terminada.Id, futura.Id);
        resultado.Valor![0].Completable.Should().BeTrue();
        resultado.Valor![1].Completable.Should().BeFalse();
    }

    [Fact]
    public async Task Una_reserva_a_las_23_30_locales_cae_en_su_dia_aunque_en_utc_sea_otro()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva noche = escenario.Reserva(new DateTime(2026, 10, 3, 21, 30, 0, DateTimeKind.Utc));
        Reserva madrugada = escenario.Reserva(new DateTime(2026, 10, 3, 22, 30, 0, DateTimeKind.Utc));
        await EscenarioEmpresa.GuardarAsync(baseDatos, noche, madrugada);

        Resultado<IReadOnlyList<ReservaEmpresaDto>> hoy = await ConsultarAsync(
            escenario.Empresa.Id, new ConsultarReservasEmpresaComando(Hoy, null, null));
        Resultado<IReadOnlyList<ReservaEmpresaDto>> manana = await ConsultarAsync(
            escenario.Empresa.Id, new ConsultarReservasEmpresaComando(Hoy.AddDays(1), null, null));

        hoy.Valor!.Select(r => r.Id).Should().Equal(noche.Id);
        manana.Valor!.Select(r => r.Id).Should().Equal(madrugada.Id);
    }

    [Fact]
    public async Task Filtra_por_profesional_y_estado()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Profesional otroProfesional = EscenarioReserva.ProfesionalQuePresta(escenario.Empresa.Id, escenario.Servicio.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, otroProfesional);
        Reserva confirmada = escenario.Reserva(DominioPrueba.Ahora.AddHours(1));
        Reserva cancelada = escenario.Reserva(DominioPrueba.Ahora.AddHours(2));
        cancelada.CancelarPorEmpresa(DominioPrueba.Ahora, OrigenCancelacion.Empresa);
        Reserva canceladaDeOtro = Reserva.Crear(
            Guid.CreateVersion7(), escenario.Empresa.Id, escenario.Cliente.Id, otroProfesional.Id, escenario.Servicio,
            DominioPrueba.Ahora.AddHours(3), new NombrePersona("Lucia Fernandez"), new Telefono(EscenarioReserva.TelefonoCliente),
            null, DominioPrueba.Ahora);
        canceladaDeOtro.CancelarPorEmpresa(DominioPrueba.Ahora, OrigenCancelacion.Empresa);
        await EscenarioEmpresa.GuardarAsync(baseDatos, confirmada, cancelada, canceladaDeOtro);

        Resultado<IReadOnlyList<ReservaEmpresaDto>> resultado = await ConsultarAsync(
            escenario.Empresa.Id, new ConsultarReservasEmpresaComando(Hoy, escenario.Profesional.Id, EstadoReserva.Cancelada));

        resultado.Valor!.Select(r => r.Id).Should().Equal(cancelada.Id);
    }

    [Fact]
    public async Task Un_estado_fuera_de_la_enumeracion_da_validacion()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);

        Resultado<IReadOnlyList<ReservaEmpresaDto>> resultado = await ConsultarAsync(
            escenario.Empresa.Id, new ConsultarReservasEmpresaComando(Hoy, null, (EstadoReserva)99));

        resultado.Codigo.Should().Be(CodigoError.Validacion);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Una_fecha_en_el_extremo_del_calendario_da_validacion_en_lugar_de_lanzar(bool maxima)
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        DateOnly fecha = maxima ? DateOnly.MaxValue : DateOnly.MinValue;

        Resultado<IReadOnlyList<ReservaEmpresaDto>> resultado = await ConsultarAsync(
            escenario.Empresa.Id, new ConsultarReservasEmpresaComando(fecha, null, null));

        resultado.Codigo.Should().Be(CodigoError.Validacion);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task La_fecha_contigua_al_extremo_del_calendario_se_consulta_sin_error(bool maxima)
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        DateOnly fecha = maxima ? DateOnly.MaxValue.AddDays(-1) : DateOnly.MinValue.AddDays(1);

        Resultado<IReadOnlyList<ReservaEmpresaDto>> resultado = await ConsultarAsync(
            escenario.Empresa.Id, new ConsultarReservasEmpresaComando(fecha, null, null));

        resultado.EsExito.Should().BeTrue();
        resultado.Valor.Should().BeEmpty();
    }

    [Fact]
    public async Task Un_profesional_de_otra_empresa_como_filtro_devuelve_una_lista_vacia()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        EscenarioReserva otra = await EscenarioReserva.CrearAsync(baseDatos);
        await EscenarioEmpresa.GuardarAsync(
            baseDatos, otra.Reserva(DominioPrueba.Ahora.AddHours(1)), escenario.Reserva(DominioPrueba.Ahora.AddHours(1)));

        Resultado<IReadOnlyList<ReservaEmpresaDto>> resultado = await ConsultarAsync(
            escenario.Empresa.Id, new ConsultarReservasEmpresaComando(Hoy, otra.Profesional.Id, null));

        resultado.EsExito.Should().BeTrue();
        resultado.Valor.Should().BeEmpty();
    }

    private async Task<Resultado<IReadOnlyList<ReservaEmpresaDto>>> ConsultarAsync(Guid empresaId, ConsultarReservasEmpresaComando comando)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        return await ambito.ServiceProvider.GetRequiredService<ConsultarReservasEmpresa>().EjecutarAsync(comando, CancellationToken.None);
    }
}
