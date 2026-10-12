using System.Globalization;
using FluentAssertions;
using PlataformaReservas.Aplicacion.Disponibilidad;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.PruebasUnitarias.Infraestructura;

namespace PlataformaReservas.PruebasUnitarias.Disponibilidad;

public sealed class PruebasMotorDisponibilidad
{
    private static readonly DateTime Ahora = new(2026, 10, 5, 6, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Miercoles = new(2026, 10, 7);
    private const int DesfaseVerano = 2;

    [Fact]
    public async Task Caso01_sin_horario_ese_dia_no_hay_huecos()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, intervalo: 45);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Tuesday, "09:00", "14:00");

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(Consultas(empresa, servicio, profesional), empresa, servicio, Miercoles);

        huecos.Should().BeEmpty();
    }

    [Fact]
    public async Task Caso02_horario_simple_sin_reservas_da_los_huecos_completos_y_correctos()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, intervalo: 45);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Wednesday, "09:00", "14:00");

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(Consultas(empresa, servicio, profesional), empresa, servicio, Miercoles);

        huecos.Select(h => h.InicioUtc).Should().Equal(EnUtc("09:00", "09:45", "10:30", "11:15", "12:00", "12:45"));
        huecos.Should().OnlyContain(h =>
            h.InicioUtc.Kind == DateTimeKind.Utc && h.FinUtc.Kind == DateTimeKind.Utc
            && h.FinUtc == h.InicioUtc.AddMinutes(45)
            && h.ProfesionalId == profesional.Id && h.NombreProfesional == "Ana Garcia");
        huecos[^1].FinUtc.Should().BeOnOrBefore(EnUtc("14:00")[0]);
    }

    [Fact]
    public async Task Caso03_jornada_partida_da_dos_bloques_sin_huecos_en_el_intermedio()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, intervalo: 45);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Wednesday, "16:00", "18:00");
        Horario(profesional, DayOfWeek.Wednesday, "09:00", "11:00");

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(Consultas(empresa, servicio, profesional), empresa, servicio, Miercoles);

        huecos.Select(h => h.InicioUtc).Should().Equal(EnUtc("09:00", "09:45", "16:00", "16:45"));
    }

    [Fact]
    public async Task Caso04_una_excepcion_ese_dia_anula_los_huecos()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, intervalo: 45);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Wednesday, "09:00", "14:00");
        profesional.MarcarNoDisponible(Guid.CreateVersion7(), Miercoles, new MotivoExcepcion("Vacaciones"));

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(Consultas(empresa, servicio, profesional), empresa, servicio, Miercoles);

        huecos.Should().BeEmpty();
    }

    [Fact]
    public async Task Caso04_una_excepcion_en_otra_fecha_no_afecta()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, intervalo: 45);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Wednesday, "09:00", "14:00");
        profesional.MarcarNoDisponible(Guid.CreateVersion7(), Miercoles.AddDays(1), null);

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(Consultas(empresa, servicio, profesional), empresa, servicio, Miercoles);

        huecos.Should().HaveCount(6);
    }

    [Fact]
    public async Task Caso05_una_reserva_en_medio_excluye_los_huecos_que_se_solapan()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, intervalo: 45);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Wednesday, "09:00", "14:00");
        Reserva reserva = NuevaReserva(empresa, profesional, EnUtc("10:00")[0], 60);

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(
            Consultas(empresa, servicio, profesional).Con(reserva), empresa, servicio, Miercoles);

        huecos.Select(h => h.InicioUtc).Should().Equal(EnUtc("09:00", "11:15", "12:00", "12:45"));
    }

    [Fact]
    public async Task Caso05_una_reserva_cancelada_no_bloquea()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, intervalo: 45);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Wednesday, "09:00", "14:00");
        Reserva cancelada = NuevaReserva(empresa, profesional, EnUtc("10:00")[0], 60);
        cancelada.CancelarPorEmpresa(Ahora, OrigenCancelacion.Empresa);

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(
            Consultas(empresa, servicio, profesional).Con(cancelada), empresa, servicio, Miercoles);

        huecos.Should().HaveCount(6);
    }

    [Fact]
    public async Task Caso05_la_reserva_de_otro_profesional_no_bloquea()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, intervalo: 45);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Wednesday, "09:00", "14:00");
        Profesional otro = NuevoProfesional(empresa, servicio, "Bruno Diaz");
        Reserva reservaDeOtro = NuevaReserva(empresa, otro, EnUtc("10:00")[0], 60);

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(
            Consultas(empresa, servicio, profesional, otro).Con(reservaDeOtro), empresa, servicio, Miercoles);

        huecos.Select(h => h.InicioUtc).Should().Equal(EnUtc("09:00", "09:45", "10:30", "11:15", "12:00", "12:45"));
    }

    [Fact]
    public async Task Caso06_reservas_al_inicio_y_al_final_del_horario_excluyen_solo_los_huecos_afectados()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, intervalo: 45);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Wednesday, "09:00", "14:00");
        Reserva alInicio = NuevaReserva(empresa, profesional, EnUtc("09:00")[0], 45);
        Reserva alFinal = NuevaReserva(empresa, profesional, EnUtc("13:00")[0], 60);

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(
            Consultas(empresa, servicio, profesional).Con(alInicio, alFinal), empresa, servicio, Miercoles);

        huecos.Select(h => h.InicioUtc).Should().Equal(EnUtc("09:45", "10:30", "11:15", "12:00"));
    }

    [Theory]
    [InlineData("09:30")]
    [InlineData("09:44")]
    public async Task Caso07_un_horario_mas_corto_que_el_servicio_no_emite_huecos(string fin)
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, intervalo: 45);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Wednesday, "09:00", fin);

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(Consultas(empresa, servicio, profesional), empresa, servicio, Miercoles);

        huecos.Should().BeEmpty();
    }

    [Fact]
    public async Task Caso07_un_servicio_que_cabe_justo_emite_un_hueco()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, intervalo: 45);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Wednesday, "09:00", "09:45");

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(Consultas(empresa, servicio, profesional), empresa, servicio, Miercoles);

        huecos.Select(h => h.InicioUtc).Should().Equal(EnUtc("09:00"));
    }

    [Fact]
    public async Task Caso07_el_ultimo_candidato_que_no_cabe_hasta_el_fin_no_se_emite()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, intervalo: 45);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Wednesday, "09:00", "10:20");

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(Consultas(empresa, servicio, profesional), empresa, servicio, Miercoles);

        huecos.Select(h => h.InicioUtc).Should().Equal(EnUtc("09:00"));
    }

    [Fact]
    public async Task Caso08_reservas_consecutivas_con_limites_exactos_no_producen_falsos_solapes()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, intervalo: 45);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Wednesday, "09:00", "14:00");
        Reserva primera = NuevaReserva(empresa, profesional, EnUtc("10:30")[0], 45);
        Reserva segunda = NuevaReserva(empresa, profesional, EnUtc("11:15")[0], 45);

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(
            Consultas(empresa, servicio, profesional).Con(primera, segunda), empresa, servicio, Miercoles);

        huecos.Select(h => h.InicioUtc).Should().Equal(EnUtc("09:00", "09:45", "12:00", "12:45"));
    }

    [Fact]
    public async Task Caso09_un_intervalo_menor_que_la_duracion_genera_huecos_solapados_entre_si()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, intervalo: 15);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Wednesday, "09:00", "14:00");

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(Consultas(empresa, servicio, profesional), empresa, servicio, Miercoles);

        huecos.Should().HaveCount(18);
        huecos.Select(h => h.InicioUtc).Should().Equal(
            Enumerable.Range(0, 18).Select(i => EnUtc("09:00")[0].AddMinutes(15 * i)));
        huecos[0].FinUtc.Should().BeAfter(huecos[1].InicioUtc);
        huecos[^1].InicioUtc.Should().Be(EnUtc("13:15")[0]);
    }

    [Fact]
    public async Task Caso10_sin_sobrescritura_el_intervalo_se_hereda_de_la_empresa()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Wednesday, "09:00", "14:00");

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(Consultas(empresa, servicio, profesional), empresa, servicio, Miercoles);

        empresa.Politicas.IntervaloHuecosMinutos.Should().Be(15);
        huecos.Should().HaveCount(18);
        huecos.Take(2).Select(h => h.InicioUtc).Should().Equal(EnUtc("09:00", "09:15"));
    }

    [Fact]
    public async Task Caso10_un_cambio_de_politicas_de_la_empresa_se_hereda_en_el_servicio()
    {
        Empresa empresa = NuevaEmpresa();
        empresa.CambiarPoliticas(new PoliticasReserva(30, 60, 60), Ahora);
        Servicio servicio = NuevoServicio(empresa);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Wednesday, "09:00", "14:00");

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(Consultas(empresa, servicio, profesional), empresa, servicio, Miercoles);

        huecos.Select(h => h.InicioUtc).Should().Equal(
            EnUtc("09:00", "09:30", "10:00", "10:30", "11:00", "11:30", "12:00", "12:30", "13:00"));
    }

    [Fact]
    public async Task Caso10_el_intervalo_sobrescrito_en_el_servicio_prevalece_sobre_el_de_la_empresa()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, intervalo: 45);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Wednesday, "09:00", "14:00");

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(Consultas(empresa, servicio, profesional), empresa, servicio, Miercoles);

        empresa.Politicas.IntervaloHuecosMinutos.Should().Be(15);
        huecos.Should().HaveCount(6);
    }

    [Fact]
    public async Task Caso11_la_antelacion_minima_heredada_admite_el_hueco_que_empieza_justo_en_el_limite()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Wednesday, "09:00", "14:00");
        DateTime ahora = EnUtc("08:15")[0];

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(
            Consultas(empresa, servicio, profesional), empresa, servicio, Miercoles, ahora: ahora);

        empresa.Politicas.AntelacionMinimaMinutos.Should().Be(60);
        huecos[0].InicioUtc.Should().Be(EnUtc("09:15")[0]);
        huecos.Should().OnlyContain(h => h.InicioUtc >= ahora.AddMinutes(60));
    }

    [Fact]
    public async Task Caso11_la_antelacion_minima_sobrescrita_en_el_servicio_prevalece()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, antelacion: 120);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Wednesday, "09:00", "14:00");

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(
            Consultas(empresa, servicio, profesional), empresa, servicio, Miercoles, ahora: EnUtc("08:15")[0]);

        huecos[0].InicioUtc.Should().Be(EnUtc("10:15")[0]);
    }

    [Fact]
    public async Task Caso11_una_fecha_pasada_no_tiene_huecos()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        HorarioTodaLaSemana(profesional);

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(
            Consultas(empresa, servicio, profesional), empresa, servicio, new DateOnly(2026, 10, 4));

        huecos.Should().BeEmpty();
    }

    [Theory]
    [InlineData(60, true)]
    [InlineData(61, false)]
    public async Task Caso11_la_ventana_maxima_heredada_admite_hoy_mas_la_ventana_y_ni_un_dia_mas(int dias, bool hayHuecos)
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        HorarioTodaLaSemana(profesional);

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(
            Consultas(empresa, servicio, profesional), empresa, servicio, new DateOnly(2026, 10, 5).AddDays(dias));

        empresa.Politicas.AntelacionMaximaDias.Should().Be(60);
        huecos.Any().Should().Be(hayHuecos);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public async Task Caso11_la_ventana_maxima_sobrescrita_en_el_servicio_prevalece(int dias, bool hayHuecos)
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, ventana: 1);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        HorarioTodaLaSemana(profesional);

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(
            Consultas(empresa, servicio, profesional), empresa, servicio, new DateOnly(2026, 10, 5).AddDays(dias));

        huecos.Any().Should().Be(hayHuecos);
    }

    [Fact]
    public async Task Caso11_hoy_es_la_fecha_local_de_madrid_y_no_la_fecha_utc()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, ventana: 1);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        HorarioTodaLaSemana(profesional);
        DateTime ahora = new(2026, 10, 5, 22, 30, 0, DateTimeKind.Utc);

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(
            Consultas(empresa, servicio, profesional), empresa, servicio, Miercoles, ahora: ahora);

        huecos.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Caso12_varios_profesionales_solo_los_activos_y_asociados_agregados_y_ordenados_por_inicio_y_nombre()
    {
        Empresa empresa = NuevaEmpresa();
        Empresa otraEmpresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, duracion: 60, intervalo: 60);
        Servicio otroServicio = NuevoServicio(empresa);
        Profesional bruno = NuevoProfesional(empresa, servicio, "Bruno Diaz");
        Horario(bruno, DayOfWeek.Wednesday, "09:00", "11:00");
        Profesional ana = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(ana, DayOfWeek.Wednesday, "10:00", "12:00");
        Profesional inactiva = NuevoProfesional(empresa, servicio, "Carla Ruiz");
        Horario(inactiva, DayOfWeek.Wednesday, "09:00", "14:00");
        inactiva.Desactivar();
        Profesional noAsociado = NuevoProfesional(empresa, otroServicio, "David Gil");
        Horario(noAsociado, DayOfWeek.Wednesday, "09:00", "14:00");
        Profesional deOtraEmpresa = NuevoProfesional(otraEmpresa, servicio, "Elena Mora");
        Horario(deOtraEmpresa, DayOfWeek.Wednesday, "09:00", "14:00");

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(
            Consultas(empresa, servicio, bruno, ana, inactiva, noAsociado, deOtraEmpresa).Con(otraEmpresa).Con(otroServicio),
            empresa, servicio, Miercoles);

        huecos.Select(h => (h.InicioUtc, h.NombreProfesional)).Should().Equal(
            (EnUtc("09:00")[0], "Bruno Diaz"),
            (EnUtc("10:00")[0], "Ana Garcia"),
            (EnUtc("10:00")[0], "Bruno Diaz"),
            (EnUtc("11:00")[0], "Ana Garcia"));
    }

    [Fact]
    public async Task Caso12_en_empate_el_nombre_con_tilde_se_ordena_por_su_letra_y_no_por_su_codigo()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, duracion: 60, intervalo: 60);
        Profesional zoe = NuevoProfesional(empresa, servicio, "Zoe Martin");
        Horario(zoe, DayOfWeek.Wednesday, "09:00", "10:00");
        Profesional alvaro = NuevoProfesional(empresa, servicio, "Álvaro Ruiz");
        Horario(alvaro, DayOfWeek.Wednesday, "09:00", "10:00");

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(
            Consultas(empresa, servicio, zoe, alvaro), empresa, servicio, Miercoles);

        huecos.Select(h => h.NombreProfesional).Should().Equal("Álvaro Ruiz", "Zoe Martin");
    }

    [Fact]
    public async Task Caso12_el_filtro_por_profesional_devuelve_solo_sus_huecos()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, duracion: 60, intervalo: 60);
        Profesional bruno = NuevoProfesional(empresa, servicio, "Bruno Diaz");
        Horario(bruno, DayOfWeek.Wednesday, "09:00", "11:00");
        Profesional ana = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(ana, DayOfWeek.Wednesday, "10:00", "12:00");

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(
            Consultas(empresa, servicio, bruno, ana), empresa, servicio, Miercoles, bruno.Id);

        huecos.Should().OnlyContain(h => h.ProfesionalId == bruno.Id);
        huecos.Select(h => h.InicioUtc).Should().Equal(EnUtc("09:00", "10:00"));
    }

    [Fact]
    public async Task Caso12_el_filtro_por_un_profesional_inactivo_no_devuelve_huecos()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, intervalo: 45);
        Profesional inactiva = NuevoProfesional(empresa, servicio, "Carla Ruiz");
        Horario(inactiva, DayOfWeek.Wednesday, "09:00", "14:00");
        inactiva.Desactivar();

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(
            Consultas(empresa, servicio, inactiva), empresa, servicio, Miercoles, inactiva.Id);

        huecos.Should().BeEmpty();
    }

    [Theory]
    [InlineData("2027-03-27", 8)]
    [InlineData("2027-03-28", 7)]
    [InlineData("2026-10-24", 7)]
    [InlineData("2026-10-25", 8)]
    public async Task Caso13_en_los_dias_del_cambio_de_hora_los_huecos_siguen_alineados_con_el_horario_local(string dia, int horaUtcDeLasNueve)
    {
        DateOnly fecha = DateOnly.ParseExact(dia, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, duracion: 60, intervalo: 60);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, fecha.DayOfWeek, "09:00", "14:00");
        DateTime ahora = fecha.AddDays(-7).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(
            Consultas(empresa, servicio, profesional), empresa, servicio, fecha, ahora: ahora);

        DateTime nueveLocal = fecha.ToDateTime(new TimeOnly(horaUtcDeLasNueve, 0), DateTimeKind.Utc);
        huecos.Select(h => h.InicioUtc).Should().Equal(Enumerable.Range(0, 5).Select(i => nueveLocal.AddHours(i)));
        huecos.Should().OnlyContain(h => h.FinUtc == h.InicioUtc.AddMinutes(60) && h.InicioUtc.Kind == DateTimeKind.Utc);
    }

    [Fact]
    public async Task Caso13_en_marzo_se_descartan_los_candidatos_que_empiezan_o_acaban_en_la_hora_que_no_existe()
    {
        DateOnly cambioMarzo = new(2027, 3, 28);
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, duracion: 60, intervalo: 60, antelacion: 0);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Sunday, "01:00", "04:00");

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(
            Consultas(empresa, servicio, profesional), empresa, servicio, cambioMarzo, ahora: new DateTime(2027, 3, 21, 0, 0, 0, DateTimeKind.Utc));

        huecos.Select(h => h.InicioUtc).Should().Equal(new DateTime(2027, 3, 28, 1, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Caso13_en_octubre_la_hora_repetida_se_convierte_con_la_conversion_estandar()
    {
        DateOnly cambioOctubre = new(2026, 10, 25);
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, duracion: 60, intervalo: 60, antelacion: 0);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Sunday, "01:00", "04:00");

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(
            Consultas(empresa, servicio, profesional), empresa, servicio, cambioOctubre, ahora: new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc));

        huecos.Select(h => h.InicioUtc).Should().Equal(
            new DateTime(2026, 10, 24, 23, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 25, 1, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 25, 2, 0, 0, DateTimeKind.Utc));
        huecos.Should().OnlyContain(h => h.FinUtc == h.InicioUtc.AddMinutes(60));
    }

    [Fact]
    public async Task Caso14_una_empresa_inactiva_no_tiene_huecos()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, intervalo: 45);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Wednesday, "09:00", "14:00");
        empresa.DarDeBaja(Ahora);

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(Consultas(empresa, servicio, profesional), empresa, servicio, Miercoles);

        huecos.Should().BeEmpty();
    }

    [Fact]
    public async Task Caso14_una_empresa_inexistente_no_tiene_huecos()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, intervalo: 45);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Wednesday, "09:00", "14:00");

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(
            new ConsultasDisponibilidadEnMemoria().Con(servicio).Con(profesional), empresa, servicio, Miercoles);

        huecos.Should().BeEmpty();
    }

    [Fact]
    public async Task Caso14_un_servicio_inactivo_no_tiene_huecos()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, intervalo: 45);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Wednesday, "09:00", "14:00");
        servicio.Desactivar(Ahora);

        IReadOnlyList<HuecoDisponible> huecos = await HuecosAsync(Consultas(empresa, servicio, profesional), empresa, servicio, Miercoles);

        huecos.Should().BeEmpty();
    }

    [Fact]
    public async Task Caso14_un_servicio_inexistente_no_tiene_huecos()
    {
        Empresa empresa = NuevaEmpresa();
        Servicio servicio = NuevoServicio(empresa, intervalo: 45);
        Profesional profesional = NuevoProfesional(empresa, servicio, "Ana Garcia");
        Horario(profesional, DayOfWeek.Wednesday, "09:00", "14:00");

        IReadOnlyList<HuecoDisponible> huecos = await new MotorDisponibilidad(
                new ConsultasDisponibilidadEnMemoria().Con(empresa).Con(profesional), new RelojFijo(Ahora))
            .ObtenerHuecosAsync(empresa.Id, servicio.Id, null, Miercoles, CancellationToken.None);

        huecos.Should().BeEmpty();
    }

    [Fact]
    public async Task Caso14_un_servicio_de_otra_empresa_no_tiene_huecos()
    {
        Empresa empresa = NuevaEmpresa();
        Empresa otraEmpresa = NuevaEmpresa();
        Servicio servicioAjeno = NuevoServicio(otraEmpresa, intervalo: 45);
        Profesional profesional = NuevoProfesional(empresa, servicioAjeno, "Ana Garcia");
        Horario(profesional, DayOfWeek.Wednesday, "09:00", "14:00");

        IReadOnlyList<HuecoDisponible> huecos = await new MotorDisponibilidad(
                Consultas(empresa, servicioAjeno, profesional).Con(otraEmpresa), new RelojFijo(Ahora))
            .ObtenerHuecosAsync(empresa.Id, servicioAjeno.Id, null, Miercoles, CancellationToken.None);

        huecos.Should().BeEmpty();
    }

    private static Empresa NuevaEmpresa()
    {
        return Empresa.Crear(
            Guid.CreateVersion7(), new NombreEmpresa("Barberia Centro"), Slug.Desde($"empresa-{Guid.CreateVersion7():N}"), Guid.CreateVersion7(),
            null, new Email("contacto@barberia.es"), new Telefono("600000000"), new Direccion("Calle Mayor 1"),
            new CodigoPostal("37001"), new Ciudad("Salamanca"), Ahora);
    }

    private static Servicio NuevoServicio(Empresa empresa, int duracion = 45, int? intervalo = null, int? antelacion = null, int? ventana = null)
    {
        Servicio servicio = Servicio.Crear(
            Guid.CreateVersion7(), empresa.Id, new NombreServicio("Corte y barba"), new Duracion(duracion), new Precio(18.50m), Ahora);
        servicio.EstablecerPoliticas(
            intervalo is null ? null : new IntervaloHuecos(intervalo.Value),
            antelacion is null ? null : new AntelacionMinimaReserva(antelacion.Value),
            ventana is null ? null : new AntelacionMaximaReserva(ventana.Value),
            Ahora);
        return servicio;
    }

    private static Profesional NuevoProfesional(Empresa empresa, Servicio servicio, string nombre)
    {
        Profesional profesional = Profesional.Crear(Guid.CreateVersion7(), empresa.Id, new NombrePersona(nombre), Ahora);
        profesional.AsociarServicio(servicio.Id);
        return profesional;
    }

    private static void Horario(Profesional profesional, DayOfWeek dia, string inicio, string fin)
    {
        profesional.AgregarIntervalo(Guid.CreateVersion7(), dia, new IntervaloHorario(Hora(inicio), Hora(fin)));
    }

    private static void HorarioTodaLaSemana(Profesional profesional)
    {
        foreach (DayOfWeek dia in Enum.GetValues<DayOfWeek>())
        {
            Horario(profesional, dia, "09:00", "14:00");
        }
    }

    private static Reserva NuevaReserva(Empresa empresa, Profesional profesional, DateTime inicioUtc, int duracion)
    {
        Servicio reservado = Servicio.Crear(
            Guid.CreateVersion7(), empresa.Id, new NombreServicio("Reservado"), new Duracion(duracion), new Precio(10m), Ahora);
        return Reserva.Crear(
            Guid.CreateVersion7(), empresa.Id, Guid.CreateVersion7(), profesional.Id, reservado, inicioUtc,
            new NombrePersona("Lucia Perez"), new Telefono("611222333"), null, Ahora);
    }

    private static ConsultasDisponibilidadEnMemoria Consultas(Empresa empresa, Servicio servicio, params Profesional[] profesionales)
    {
        return new ConsultasDisponibilidadEnMemoria().Con(empresa).Con(servicio).Con(profesionales);
    }

    private static Task<IReadOnlyList<HuecoDisponible>> HuecosAsync(
        ConsultasDisponibilidadEnMemoria consultas, Empresa empresa, Servicio servicio, DateOnly fecha,
        Guid? profesionalId = null, DateTime? ahora = null)
    {
        return new MotorDisponibilidad(consultas, new RelojFijo(ahora ?? Ahora))
            .ObtenerHuecosAsync(empresa.Id, servicio.Id, profesionalId, fecha, CancellationToken.None);
    }

    private static TimeOnly Hora(string hora)
    {
        return TimeOnly.ParseExact(hora, "HH:mm", CultureInfo.InvariantCulture);
    }

    private static DateTime[] EnUtc(params string[] horasLocalesDelMiercoles)
    {
        return horasLocalesDelMiercoles
            .Select(h => Miercoles.ToDateTime(Hora(h), DateTimeKind.Utc).AddHours(-DesfaseVerano))
            .ToArray();
    }
}
