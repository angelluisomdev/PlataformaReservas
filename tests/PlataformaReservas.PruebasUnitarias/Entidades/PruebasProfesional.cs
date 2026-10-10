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

    [Fact]
    public void AgregarIntervalo_admite_jornada_partida_y_los_hijos_heredan_empresa_y_profesional()
    {
        Profesional profesional = CrearProfesional();

        profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Monday, Intervalo(9, 14));
        profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Monday, Intervalo(16, 20));

        profesional.Horarios.Should().HaveCount(2).And.OnlyContain(h =>
            h.DiaSemana == DayOfWeek.Monday && h.EmpresaId == profesional.EmpresaId && h.ProfesionalId == profesional.Id);
    }

    [Fact]
    public void AgregarIntervalo_rechaza_un_solape_el_mismo_dia_y_lo_admite_otro_dia()
    {
        Profesional profesional = CrearProfesional();
        profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Monday, Intervalo(9, 14));

        Action solapado = () => profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Monday, Intervalo(13, 16));
        profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Tuesday, Intervalo(13, 16));

        solapado.Should().Throw<DominioException>();
        profesional.Horarios.Should().HaveCount(2);
    }

    [Fact]
    public void AgregarIntervalo_admite_contiguos_y_el_que_encaja_en_el_hueco()
    {
        Profesional profesional = CrearProfesional();
        profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Monday, Intervalo(9, 14));
        profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Monday, Intervalo(16, 20));

        profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Monday, Intervalo(14, 16));
        profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Monday, Intervalo(20, 22));

        profesional.Horarios.Should().HaveCount(4);
    }

    [Fact]
    public void AgregarIntervalo_compara_con_todos_los_intervalos_del_dia()
    {
        Profesional profesional = CrearProfesional();
        profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Monday, Intervalo(9, 14));
        profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Monday, Intervalo(16, 20));

        Action conElSegundo = () => profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Monday, Intervalo(15, 17));

        conElSegundo.Should().Throw<DominioException>();
        profesional.Horarios.Should().HaveCount(2);
    }

    [Fact]
    public void AgregarIntervalo_y_MarcarNoDisponible_rechazan_un_identificador_vacio()
    {
        Profesional profesional = CrearProfesional();

        Action intervalo = () => profesional.AgregarIntervalo(Guid.Empty, DayOfWeek.Monday, Intervalo(9, 14));
        Action excepcion = () => profesional.MarcarNoDisponible(Guid.Empty, new DateOnly(2026, 12, 24), null);

        intervalo.Should().Throw<DominioException>();
        excepcion.Should().Throw<DominioException>();
        profesional.Horarios.Should().BeEmpty();
        profesional.Excepciones.Should().BeEmpty();
    }

    [Fact]
    public void EliminarIntervalo_lo_retira_y_rechaza_uno_que_no_existe()
    {
        Profesional profesional = CrearProfesional();
        Guid horarioId = Guid.CreateVersion7();
        profesional.AgregarIntervalo(horarioId, DayOfWeek.Monday, Intervalo(9, 14));

        profesional.EliminarIntervalo(horarioId);
        Action otraVez = () => profesional.EliminarIntervalo(horarioId);

        profesional.Horarios.Should().BeEmpty();
        otraVez.Should().Throw<DominioException>();
    }

    [Fact]
    public void EliminarIntervalo_retira_solo_ese_y_libera_el_hueco()
    {
        Profesional profesional = CrearProfesional();
        Guid manana = Guid.CreateVersion7();
        Guid tarde = Guid.CreateVersion7();
        profesional.AgregarIntervalo(manana, DayOfWeek.Monday, Intervalo(9, 14));
        profesional.AgregarIntervalo(tarde, DayOfWeek.Monday, Intervalo(16, 20));

        profesional.EliminarIntervalo(manana);
        profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Monday, Intervalo(9, 14));

        profesional.Horarios.Should().HaveCount(2).And.Contain(h => h.Id == tarde).And.NotContain(h => h.Id == manana);
    }

    [Fact]
    public void MarcarNoDisponible_admite_fechas_distintas()
    {
        Profesional profesional = CrearProfesional();

        profesional.MarcarNoDisponible(Guid.CreateVersion7(), new DateOnly(2026, 12, 24), null);
        profesional.MarcarNoDisponible(Guid.CreateVersion7(), new DateOnly(2026, 12, 25), null);

        profesional.Excepciones.Select(x => x.Fecha).Should().BeEquivalentTo([new DateOnly(2026, 12, 24), new DateOnly(2026, 12, 25)]);
    }

    [Fact]
    public void QuitarExcepcion_retira_solo_esa_fecha_y_permite_volver_a_marcarla()
    {
        Profesional profesional = CrearProfesional();
        DateOnly nochebuena = new(2026, 12, 24);
        DateOnly navidad = new(2026, 12, 25);
        profesional.MarcarNoDisponible(Guid.CreateVersion7(), nochebuena, null);
        profesional.MarcarNoDisponible(Guid.CreateVersion7(), navidad, null);

        profesional.QuitarExcepcion(nochebuena);
        profesional.MarcarNoDisponible(Guid.CreateVersion7(), nochebuena, new MotivoExcepcion("Otra vez"));

        profesional.Excepciones.Should().HaveCount(2);
        profesional.Excepciones.Single(x => x.Fecha == nochebuena).Motivo.Should().Be(new MotivoExcepcion("Otra vez"));
    }

    [Fact]
    public void MarcarNoDisponible_guarda_fecha_y_motivo_y_rechaza_una_segunda_el_mismo_dia()
    {
        Profesional profesional = CrearProfesional();
        DateOnly navidad = new(2026, 12, 25);

        profesional.MarcarNoDisponible(Guid.CreateVersion7(), navidad, new MotivoExcepcion("Festivo"));
        Action segunda = () => profesional.MarcarNoDisponible(Guid.CreateVersion7(), navidad, null);

        segunda.Should().Throw<DominioException>();
        ExcepcionHorario excepcion = profesional.Excepciones.Should().ContainSingle().Subject;
        excepcion.Fecha.Should().Be(navidad);
        excepcion.Motivo.Should().Be(new MotivoExcepcion("Festivo"));
        excepcion.EmpresaId.Should().Be(profesional.EmpresaId);
        excepcion.ProfesionalId.Should().Be(profesional.Id);
    }

    [Fact]
    public void QuitarExcepcion_la_retira_y_rechaza_una_fecha_sin_excepcion()
    {
        Profesional profesional = CrearProfesional();
        DateOnly navidad = new(2026, 12, 25);
        profesional.MarcarNoDisponible(Guid.CreateVersion7(), navidad, null);

        profesional.QuitarExcepcion(navidad);
        Action otraVez = () => profesional.QuitarExcepcion(navidad);

        profesional.Excepciones.Should().BeEmpty();
        otraVez.Should().Throw<DominioException>();
    }

    private static IntervaloHorario Intervalo(int horaInicio, int horaFin)
    {
        return new IntervaloHorario(new TimeOnly(horaInicio, 0), new TimeOnly(horaFin, 0));
    }
}
