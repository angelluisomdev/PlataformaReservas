using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Empresas;
using PlataformaReservas.Aplicacion.Dtos.Profesionales;
using PlataformaReservas.Aplicacion.Dtos.Reservas;
using PlataformaReservas.Aplicacion.Dtos.Servicios;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Consultas;

[Collection("BaseDatos")]
public sealed class PruebasConsultasDisponibilidad(BaseDatosFixture baseDatos)
{
    private static readonly DateTime Desde = new(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Hasta = new(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task La_empresa_se_obtiene_sin_contexto_de_empresa_y_una_inexistente_no_se_encuentra()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);

        EmpresaDisponibilidadDto? encontrada = await ConsultarAsync(c => c.ObtenerEmpresaAsync(empresa.Id, CancellationToken.None));
        EmpresaDisponibilidadDto? inexistente = await ConsultarAsync(c => c.ObtenerEmpresaAsync(Guid.CreateVersion7(), CancellationToken.None));

        encontrada.Should().Be(new EmpresaDisponibilidadDto(true, 15, 60, 60));
        inexistente.Should().BeNull();
    }

    [Fact]
    public async Task El_servicio_de_otra_empresa_no_se_encuentra()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Servicio servicio = DominioPrueba.Servicio(propia.Id);
        Servicio servicioAjeno = DominioPrueba.Servicio(ajena.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, servicio, servicioAjeno);

        ServicioDisponibilidadDto? propio = await ConsultarAsync(c => c.ObtenerServicioAsync(propia.Id, servicio.Id, CancellationToken.None));
        ServicioDisponibilidadDto? deOtra = await ConsultarAsync(c => c.ObtenerServicioAsync(propia.Id, servicioAjeno.Id, CancellationToken.None));

        propio.Should().Be(new ServicioDisponibilidadDto(true, 45, null, null, null));
        deOtra.Should().BeNull();
    }

    [Fact]
    public async Task Solo_devuelve_los_profesionales_activos_de_la_empresa_que_prestan_el_servicio_con_su_horario()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Servicio servicio = DominioPrueba.Servicio(propia.Id);
        Servicio otroServicio = DominioPrueba.Servicio(propia.Id);
        Profesional activo = DominioPrueba.Profesional(propia.Id);
        activo.AsociarServicio(servicio.Id);
        activo.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Saturday, new IntervaloHorario(new TimeOnly(9, 0), new TimeOnly(14, 0)));
        activo.MarcarNoDisponible(Guid.CreateVersion7(), new DateOnly(2026, 12, 25), new MotivoExcepcion("Navidad"));
        Profesional inactivo = DominioPrueba.Profesional(propia.Id);
        inactivo.AsociarServicio(servicio.Id);
        inactivo.Desactivar();
        Profesional noAsociado = DominioPrueba.Profesional(propia.Id);
        noAsociado.AsociarServicio(otroServicio.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, servicio, otroServicio, activo, inactivo, noAsociado);

        IReadOnlyList<ProfesionalDisponibilidadDto> profesionales = await ConsultarAsync(
            c => c.ObtenerProfesionalesQuePrestanAsync(propia.Id, servicio.Id, null, CancellationToken.None));

        ProfesionalDisponibilidadDto unico = profesionales.Should().ContainSingle().Subject;
        unico.Id.Should().Be(activo.Id);
        unico.NombreCompleto.Should().Be("Ana Garcia");
        unico.Horarios.Should().ContainSingle().Which.Should().Be(new IntervaloHorarioDto(
            activo.Horarios.Single().Id, DayOfWeek.Saturday, new TimeOnly(9, 0), new TimeOnly(14, 0)));
        unico.FechasNoDisponibles.Should().Equal(new DateOnly(2026, 12, 25));
    }

    [Fact]
    public async Task Los_profesionales_se_filtran_por_empresa_y_por_profesional()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Servicio servicio = DominioPrueba.Servicio(propia.Id);
        Profesional ana = DominioPrueba.Profesional(propia.Id);
        ana.AsociarServicio(servicio.Id);
        Profesional bruno = DominioPrueba.Profesional(propia.Id);
        bruno.AsociarServicio(servicio.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, servicio, ana, bruno);

        IReadOnlyList<ProfesionalDisponibilidadDto> todos = await ConsultarAsync(
            c => c.ObtenerProfesionalesQuePrestanAsync(propia.Id, servicio.Id, null, CancellationToken.None));
        IReadOnlyList<ProfesionalDisponibilidadDto> soloBruno = await ConsultarAsync(
            c => c.ObtenerProfesionalesQuePrestanAsync(propia.Id, servicio.Id, bruno.Id, CancellationToken.None));
        IReadOnlyList<ProfesionalDisponibilidadDto> desdeOtraEmpresa = await ConsultarAsync(
            c => c.ObtenerProfesionalesQuePrestanAsync(ajena.Id, servicio.Id, null, CancellationToken.None));

        todos.Select(p => p.Id).Should().BeEquivalentTo([ana.Id, bruno.Id]);
        soloBruno.Should().ContainSingle().Which.Id.Should().Be(bruno.Id);
        desdeOtraEmpresa.Should().BeEmpty();
    }

    [Fact]
    public async Task Solo_devuelve_las_reservas_no_canceladas_de_los_profesionales_pedidos_que_se_solapan_con_el_rango()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        Servicio servicio = DominioPrueba.Servicio(propia.Id);
        Servicio servicioAjeno = DominioPrueba.Servicio(ajena.Id);
        Profesional ana = DominioPrueba.Profesional(propia.Id);
        Profesional bruno = DominioPrueba.Profesional(propia.Id);
        Profesional noPedido = DominioPrueba.Profesional(propia.Id);
        Profesional ajeno = DominioPrueba.Profesional(ajena.Id);

        Reserva acabaJustoEnElInicio = Nueva(propia.Id, usuario.Id, ana.Id, servicio, Desde.AddMinutes(-45));
        Reserva empiezaEnElInicio = Nueva(propia.Id, usuario.Id, ana.Id, servicio, Desde);
        Reserva cancelada = Nueva(propia.Id, usuario.Id, ana.Id, servicio, Desde.AddMinutes(45));
        cancelada.CancelarPorEmpresa(DominioPrueba.Ahora);
        Reserva cruzaElFin = Nueva(propia.Id, usuario.Id, ana.Id, servicio, Hasta.AddMinutes(-30));
        Reserva deBrunoDentro = Nueva(propia.Id, usuario.Id, bruno.Id, servicio, Desde.AddHours(1));
        Reserva empiezaJustoEnElFin = Nueva(propia.Id, usuario.Id, bruno.Id, servicio, Hasta);
        Reserva deNoPedido = Nueva(propia.Id, usuario.Id, noPedido.Id, servicio, Desde);
        Reserva deOtraEmpresa = Nueva(ajena.Id, usuario.Id, ajeno.Id, servicioAjeno, Desde);
        await EscenarioEmpresa.GuardarAsync(
            baseDatos, servicio, servicioAjeno, ana, bruno, noPedido, ajeno,
            acabaJustoEnElInicio, empiezaEnElInicio, cancelada, cruzaElFin, deBrunoDentro, empiezaJustoEnElFin, deNoPedido, deOtraEmpresa);

        IReadOnlyList<ReservaOcupadaDto> reservas = await ConsultarAsync(
            c => c.ObtenerReservasActivasAsync(propia.Id, [ana.Id, bruno.Id, ajeno.Id], Desde, Hasta, CancellationToken.None));

        reservas.Should().BeEquivalentTo([Ocupada(empiezaEnElInicio), Ocupada(cruzaElFin), Ocupada(deBrunoDentro)]);
    }

    [Fact]
    public async Task Las_consultas_no_dejan_entidades_en_seguimiento()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        profesional.AsociarServicio(servicio.Id);
        profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Saturday, new IntervaloHorario(new TimeOnly(9, 0), new TimeOnly(14, 0)));
        profesional.MarcarNoDisponible(Guid.CreateVersion7(), new DateOnly(2026, 12, 25), null);
        Reserva reserva = Nueva(empresa.Id, usuario.Id, profesional.Id, servicio, Desde);
        await EscenarioEmpresa.GuardarAsync(baseDatos, servicio, profesional, reserva);

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        IConsultasDisponibilidad consultas = ambito.ServiceProvider.GetRequiredService<IConsultasDisponibilidad>();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();

        (await consultas.ObtenerEmpresaAsync(empresa.Id, CancellationToken.None)).Should().NotBeNull();
        (await consultas.ObtenerServicioAsync(empresa.Id, servicio.Id, CancellationToken.None)).Should().NotBeNull();
        (await consultas.ObtenerProfesionalesQuePrestanAsync(empresa.Id, servicio.Id, null, CancellationToken.None)).Should().ContainSingle();
        (await consultas.ObtenerReservasActivasAsync(empresa.Id, [profesional.Id], Desde, Hasta, CancellationToken.None)).Should().ContainSingle();

        db.ChangeTracker.Entries().Should().BeEmpty();
    }

    private static ReservaOcupadaDto Ocupada(Reserva reserva)
    {
        return new ReservaOcupadaDto(reserva.ProfesionalId, reserva.Franja.InicioUtc, reserva.Franja.FinUtc);
    }

    private static Reserva Nueva(Guid empresaId, Guid usuarioId, Guid profesionalId, Servicio servicio, DateTime inicioUtc)
    {
        return Reserva.Crear(
            Guid.CreateVersion7(), empresaId, usuarioId, profesionalId, servicio, inicioUtc,
            new NombrePersona("Lucia Fernandez"), new Telefono("699887766"), null, DominioPrueba.Ahora);
    }

    private async Task<T> ConsultarAsync<T>(Func<IConsultasDisponibilidad, Task<T>> consulta)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        return await consulta(ambito.ServiceProvider.GetRequiredService<IConsultasDisponibilidad>());
    }
}
