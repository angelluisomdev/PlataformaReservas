using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Reservas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Consultas;

[Collection("BaseDatos")]
public sealed class PruebasConsultasReservasEmpresa(BaseDatosFixture baseDatos)
{
    private static readonly DateTime Ahora = DominioPrueba.Ahora;
    private static readonly DateTime Desde = Ahora.AddHours(-2);
    private static readonly DateTime Hasta = Ahora.AddHours(10);

    [Fact]
    public async Task Solo_devuelve_reservas_de_la_empresa_del_contexto()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        EscenarioReserva otra = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva propia = escenario.Reserva(Ahora.AddHours(1));
        Reserva ajena = otra.Reserva(Ahora.AddHours(1));
        await EscenarioEmpresa.GuardarAsync(baseDatos, propia, ajena);

        IReadOnlyList<ReservaEmpresaDto> reservas = await ReservasAsync(escenario.Empresa.Id, Desde, Hasta);

        reservas.Select(r => r.Id).Should().Equal(propia.Id);
        reservas[0].Servicio.Should().Be(escenario.Servicio.Nombre.Valor);
        reservas[0].Profesional.Should().Be(escenario.Profesional.NombreCompleto.Valor);
    }

    [Fact]
    public async Task El_rango_es_semiabierto_por_el_inicio()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva enDesde = escenario.Reserva(Desde);
        Reserva antes = escenario.Reserva(Desde.AddHours(-1));
        Reserva enHasta = escenario.Reserva(Hasta);
        await EscenarioEmpresa.GuardarAsync(baseDatos, enDesde, antes, enHasta);

        IReadOnlyList<ReservaEmpresaDto> reservas = await ReservasAsync(escenario.Empresa.Id, Desde, Hasta);

        reservas.Select(r => r.Id).Should().Equal(enDesde.Id);
    }

    [Fact]
    public async Task Filtra_por_profesional_y_por_estado_y_ordena_por_inicio()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Profesional otroProfesional = EscenarioReserva.ProfesionalQuePresta(escenario.Empresa.Id, escenario.Servicio.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, otroProfesional);
        Reserva tarde = escenario.Reserva(Ahora.AddHours(3));
        Reserva pronto = escenario.Reserva(Ahora.AddHours(1));
        Reserva cancelada = escenario.Reserva(Ahora.AddHours(2));
        cancelada.CancelarPorEmpresa(Ahora, OrigenCancelacion.Empresa);
        Reserva deOtro = Reserva.Crear(
            Guid.CreateVersion7(), escenario.Empresa.Id, escenario.Cliente.Id, otroProfesional.Id, escenario.Servicio,
            Ahora.AddHours(1), new NombrePersona("Lucia Fernandez"), new Telefono(EscenarioReserva.TelefonoCliente), null, Ahora);
        await EscenarioEmpresa.GuardarAsync(baseDatos, tarde, pronto, cancelada, deOtro);

        IReadOnlyList<ReservaEmpresaDto> todas = await ReservasAsync(escenario.Empresa.Id, Desde, Hasta);
        IReadOnlyList<ReservaEmpresaDto> delProfesional = await ReservasAsync(
            escenario.Empresa.Id, Desde, Hasta, escenario.Profesional.Id);
        IReadOnlyList<ReservaEmpresaDto> confirmadasDelProfesional = await ReservasAsync(
            escenario.Empresa.Id, Desde, Hasta, escenario.Profesional.Id, EstadoReserva.Confirmada);

        todas.Select(r => r.InicioUtc).Should().BeInAscendingOrder();
        todas.Should().HaveCount(4);
        delProfesional.Select(r => r.Id).Should().Equal(pronto.Id, cancelada.Id, tarde.Id);
        confirmadasDelProfesional.Select(r => r.Id).Should().Equal(pronto.Id, tarde.Id);
    }

    [Fact]
    public async Task El_nombre_y_el_telefono_del_cliente_llegan_descifrados_y_en_la_tabla_siguen_cifrados()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva reserva = escenario.Reserva(Ahora.AddHours(1));
        await EscenarioEmpresa.GuardarAsync(baseDatos, reserva);

        ReservaEmpresaDto dto = (await ReservasAsync(escenario.Empresa.Id, Desde, Hasta)).Single();

        dto.NombreCliente.Should().Be("Lucia Fernandez");
        dto.TelefonoCliente.Should().Be(EscenarioReserva.TelefonoCliente);
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        List<string> enTabla = await ambito.ServiceProvider.GetRequiredService<ContextoDatos>().Database.SqlQuery<string>($"""
            SELECT unnest(ARRAY[nombre_cliente, telefono_cliente]) AS "Value" FROM reservas WHERE id = {reserva.Id}
            """).ToListAsync();
        enTabla.Should().HaveCount(2).And.NotContain(["Lucia Fernandez", EscenarioReserva.TelefonoCliente]);
    }

    [Fact]
    public async Task Cancelable_si_esta_confirmada_y_completable_si_ya_termino()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva terminada = escenario.Reserva(Ahora.AddMinutes(-60));
        Reserva enCurso = escenario.Reserva(Ahora.AddMinutes(-10));
        await EscenarioEmpresa.GuardarAsync(baseDatos, terminada, enCurso);

        IReadOnlyList<ReservaEmpresaDto> reservas = await ReservasAsync(escenario.Empresa.Id, Desde, Hasta);

        reservas.Should().AllSatisfy(r => r.Cancelable.Should().BeTrue());
        reservas.Single(r => r.Id == terminada.Id).Completable.Should().BeTrue();
        reservas.Single(r => r.Id == enCurso.Id).Completable.Should().BeFalse();
    }

    [Fact]
    public async Task Las_consultas_no_dejan_entidades_en_seguimiento()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        await EscenarioEmpresa.GuardarAsync(baseDatos, escenario.Reserva(Ahora.AddHours(1)));

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(escenario.Empresa.Id);
        IConsultasReservasEmpresa consultas = ambito.ServiceProvider.GetRequiredService<IConsultasReservasEmpresa>();
        await consultas.ObtenerReservasAsync(Desde, Hasta, null, null, Ahora, CancellationToken.None);
        await consultas.ObtenerPanelAsync(Desde, Hasta, Hasta.AddDays(7), Ahora, CancellationToken.None);

        ambito.ServiceProvider.GetRequiredService<ContextoDatos>().ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task El_panel_indica_uno_por_uno_los_requisitos_que_faltan_para_publicar()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        (await PanelAsync(empresa.Id)).Should().BeEquivalentTo(Requisitos(false, false, false, false));

        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, servicio);
        (await PanelAsync(empresa.Id)).Should().BeEquivalentTo(Requisitos(true, false, false, false));

        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, profesional);
        (await PanelAsync(empresa.Id)).Should().BeEquivalentTo(Requisitos(true, true, false, false));

        await ModificarProfesionalAsync(profesional.Id, p => p.AsociarServicio(servicio.Id));
        (await PanelAsync(empresa.Id)).Should().BeEquivalentTo(Requisitos(true, true, true, false));

        await ModificarProfesionalAsync(profesional.Id, p => p.AgregarIntervalo(
            Guid.CreateVersion7(), DayOfWeek.Monday, new IntervaloHorario(new TimeOnly(9, 0), new TimeOnly(14, 0))));
        (await PanelAsync(empresa.Id)).Should().BeEquivalentTo(Requisitos(true, true, true, true));
    }

    [Fact]
    public async Task Un_servicio_inactivo_no_cuenta_para_la_asociacion_ni_para_publicar()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        await EjecutarSqlAsync($"UPDATE servicios SET activo = false WHERE id = {escenario.Servicio.Id}");

        (await PanelAsync(escenario.Empresa.Id)).Should().BeEquivalentTo(Requisitos(false, true, false, false));
    }

    [Fact]
    public async Task Un_profesional_inactivo_asociado_a_un_servicio_activo_no_cuenta_como_profesional_ni_como_asociacion()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        await EjecutarSqlAsync($"UPDATE profesionales SET activo = false WHERE id = {escenario.Profesional.Id}");

        (await PanelAsync(escenario.Empresa.Id)).Should().BeEquivalentTo(Requisitos(true, false, false, false));
    }

    [Fact]
    public async Task Las_reservas_de_un_servicio_o_un_profesional_desactivados_siguen_apareciendo_y_son_cancelables()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva reserva = escenario.Reserva(Ahora.AddHours(1));
        await EscenarioEmpresa.GuardarAsync(baseDatos, reserva);
        await EjecutarSqlAsync($"UPDATE servicios SET activo = false WHERE id = {escenario.Servicio.Id}");
        await EjecutarSqlAsync($"UPDATE profesionales SET activo = false WHERE id = {escenario.Profesional.Id}");

        IReadOnlyList<ReservaEmpresaDto> reservas = await ReservasAsync(escenario.Empresa.Id, Desde, Hasta);
        PanelEmpresaDto panel = await PanelAsync(escenario.Empresa.Id);

        reservas.Should().ContainSingle().Which.Cancelable.Should().BeTrue();
        panel.Hoy.Select(r => r.Id).Should().Equal(reserva.Id);
    }

    [Fact]
    public async Task El_panel_separa_hoy_de_las_proximas_confirmadas_y_no_mezcla_otra_empresa()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        EscenarioReserva otra = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva hoy = escenario.Reserva(Ahora.AddHours(1));
        Reserva hoyCancelada = escenario.Reserva(Ahora.AddHours(2));
        hoyCancelada.CancelarPorEmpresa(Ahora, OrigenCancelacion.Empresa);
        Reserva proxima = escenario.Reserva(Hasta.AddDays(1));
        Reserva proximaCancelada = escenario.Reserva(Hasta.AddDays(2));
        proximaCancelada.CancelarPorEmpresa(Ahora, OrigenCancelacion.Empresa);
        Reserva fueraDeRango = escenario.Reserva(Hasta.AddDays(8));
        Reserva ajena = otra.Reserva(Ahora.AddHours(1));
        await EscenarioEmpresa.GuardarAsync(baseDatos, hoy, hoyCancelada, proxima, proximaCancelada, fueraDeRango, ajena);

        PanelEmpresaDto panel = await PanelAsync(escenario.Empresa.Id);

        panel.Hoy.Select(r => r.Id).Should().Equal(hoy.Id, hoyCancelada.Id);
        panel.Proximas.Select(r => r.Id).Should().Equal(proxima.Id);
    }

    [Fact]
    public async Task El_panel_ordena_hoy_y_las_proximas_por_inicio()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva hoyTarde = escenario.Reserva(Ahora.AddHours(3));
        Reserva hoyPronto = escenario.Reserva(Ahora.AddHours(1));
        Reserva proximaLejana = escenario.Reserva(Hasta.AddDays(3));
        Reserva proximaCercana = escenario.Reserva(Hasta.AddDays(1));
        await EscenarioEmpresa.GuardarAsync(baseDatos, hoyTarde, proximaLejana, hoyPronto, proximaCercana);

        PanelEmpresaDto panel = await PanelAsync(escenario.Empresa.Id);

        panel.Hoy.Select(r => r.Id).Should().Equal(hoyPronto.Id, hoyTarde.Id);
        panel.Proximas.Select(r => r.Id).Should().Equal(proximaCercana.Id, proximaLejana.Id);
    }

    [Fact]
    public async Task El_panel_de_una_empresa_inactiva_sigue_devolviendo_sus_reservas()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva reserva = escenario.Reserva(Ahora.AddHours(1));
        await EscenarioEmpresa.GuardarAsync(baseDatos, reserva);
        await EjecutarSqlAsync($"UPDATE empresas SET activa = false WHERE id = {escenario.Empresa.Id}");

        PanelEmpresaDto panel = await PanelAsync(escenario.Empresa.Id);

        panel.Activa.Should().BeFalse();
        panel.Hoy.Select(r => r.Id).Should().Equal(reserva.Id);
    }

    private static object Requisitos(bool servicio, bool profesional, bool asociacion, bool publicable)
    {
        return new
        {
            TieneServicioActivo = servicio,
            TieneProfesionalActivo = profesional,
            TieneAsociacion = asociacion,
            EsPublicable = publicable,
        };
    }

    private async Task<IReadOnlyList<ReservaEmpresaDto>> ReservasAsync(
        Guid empresaId, DateTime desde, DateTime hasta, Guid? profesionalId = null, EstadoReserva? estado = null)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        return await ambito.ServiceProvider.GetRequiredService<IConsultasReservasEmpresa>()
            .ObtenerReservasAsync(desde, hasta, profesionalId, estado, Ahora, CancellationToken.None);
    }

    private async Task<PanelEmpresaDto> PanelAsync(Guid empresaId)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        return await ambito.ServiceProvider.GetRequiredService<IConsultasReservasEmpresa>()
            .ObtenerPanelAsync(Desde, Hasta, Hasta.AddDays(7), Ahora, CancellationToken.None);
    }

    private async Task ModificarProfesionalAsync(Guid profesionalId, Action<Profesional> cambio)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();
        Profesional profesional = await db.Profesionales.Include(p => p.Horarios).AsTracking().SingleAsync(p => p.Id == profesionalId);
        cambio(profesional);
        await db.SaveChangesAsync();
    }

    private async Task EjecutarSqlAsync(FormattableString sql)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        await ambito.ServiceProvider.GetRequiredService<ContextoDatos>().Database.ExecuteSqlInterpolatedAsync(sql);
    }
}
