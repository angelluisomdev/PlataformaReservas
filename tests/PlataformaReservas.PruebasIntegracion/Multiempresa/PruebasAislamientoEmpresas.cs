using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.Infraestructura.Repositorios;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Multiempresa;

[Collection("BaseDatos")]
public sealed class PruebasAislamientoEmpresas(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Un_servicio_de_otra_empresa_no_se_encuentra_y_queda_intacto()
    {
        (Empresa a, Empresa b) = await CrearEmpresasAsync();
        Servicio servicio = DominioPrueba.Servicio(b.Id);
        await GuardarAsync(servicio);

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();
        RepositorioServicios desdeA = new(db, new ContextoEmpresaFijo(a.Id));
        RepositorioServicios desdeB = new(db, new ContextoEmpresaFijo(b.Id));

        (await desdeA.ObtenerPorIdAsync(servicio.Id, CancellationToken.None)).Should().BeNull();
        (await desdeA.ObtenerParaModificarAsync(servicio.Id, CancellationToken.None)).Should().BeNull();
        (await desdeB.ObtenerPorIdAsync(servicio.Id, CancellationToken.None)).Should().BeEquivalentTo(servicio);
    }

    [Fact]
    public async Task Un_profesional_de_otra_empresa_no_se_encuentra_y_queda_intacto()
    {
        (Empresa a, Empresa b) = await CrearEmpresasAsync();
        Profesional profesional = DominioPrueba.Profesional(b.Id);
        await GuardarAsync(profesional);

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();
        RepositorioProfesionales desdeA = new(db, new ContextoEmpresaFijo(a.Id));
        RepositorioProfesionales desdeB = new(db, new ContextoEmpresaFijo(b.Id));

        (await desdeA.ObtenerPorIdAsync(profesional.Id, CancellationToken.None)).Should().BeNull();
        (await desdeA.ObtenerParaModificarAsync(profesional.Id, CancellationToken.None)).Should().BeNull();
        (await desdeB.ObtenerPorIdAsync(profesional.Id, CancellationToken.None)).Should().BeEquivalentTo(profesional);
    }

    [Fact]
    public async Task Un_horario_de_otra_empresa_no_se_encuentra_y_queda_intacto()
    {
        (Empresa a, Empresa b) = await CrearEmpresasAsync();
        Profesional propio = DominioPrueba.Profesional(a.Id);
        Profesional ajeno = DominioPrueba.Profesional(b.Id);
        await GuardarAsync(propio, ajeno);
        Guid horarioId = Guid.CreateVersion7();
        await EjecutarSqlAsync($"""
            INSERT INTO horarios (id, empresa_id, profesional_id, dia_semana, hora_inicio, hora_fin)
            VALUES ({horarioId}, {b.Id}, {ajeno.Id}, {(int)DayOfWeek.Monday}, {new TimeOnly(9, 0)}, {new TimeOnly(14, 0)})
            """);

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();
        RepositorioProfesionales desdeA = new(db, new ContextoEmpresaFijo(a.Id));
        RepositorioProfesionales desdeB = new(db, new ContextoEmpresaFijo(b.Id));

        (await desdeA.ObtenerPorIdAsync(ajeno.Id, CancellationToken.None)).Should().BeNull();
        (await desdeA.ObtenerParaModificarAsync(ajeno.Id, CancellationToken.None)).Should().BeNull();
        (await desdeA.ObtenerParaModificarAsync(propio.Id, CancellationToken.None))!.Horarios.Should().BeEmpty();
        (await desdeB.ObtenerParaModificarAsync(ajeno.Id, CancellationToken.None))!.Horarios
            .Should().ContainSingle(h => h.Id == horarioId && h.EmpresaId == b.Id
                && h.Intervalo == new IntervaloHorario(new TimeOnly(9, 0), new TimeOnly(14, 0)));
    }

    [Fact]
    public async Task Una_excepcion_de_horario_de_otra_empresa_no_se_encuentra_y_queda_intacta()
    {
        (Empresa a, Empresa b) = await CrearEmpresasAsync();
        Profesional propio = DominioPrueba.Profesional(a.Id);
        Profesional ajeno = DominioPrueba.Profesional(b.Id);
        await GuardarAsync(propio, ajeno);
        Guid excepcionId = Guid.CreateVersion7();
        DateOnly fecha = new(2026, 10, 12);
        await EjecutarSqlAsync($"""
            INSERT INTO excepciones_horario (id, empresa_id, profesional_id, fecha, motivo)
            VALUES ({excepcionId}, {b.Id}, {ajeno.Id}, {fecha}, {"Vacaciones"})
            """);

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();
        RepositorioProfesionales desdeA = new(db, new ContextoEmpresaFijo(a.Id));
        RepositorioProfesionales desdeB = new(db, new ContextoEmpresaFijo(b.Id));

        (await desdeA.ObtenerPorIdAsync(ajeno.Id, CancellationToken.None)).Should().BeNull();
        (await desdeA.ObtenerParaModificarAsync(ajeno.Id, CancellationToken.None)).Should().BeNull();
        (await desdeA.ObtenerParaModificarAsync(propio.Id, CancellationToken.None))!.Excepciones.Should().BeEmpty();
        (await desdeB.ObtenerParaModificarAsync(ajeno.Id, CancellationToken.None))!.Excepciones
            .Should().ContainSingle(x => x.Id == excepcionId && x.EmpresaId == b.Id && x.Fecha == fecha && x.Motivo == new MotivoExcepcion("Vacaciones"));
    }

    [Fact]
    public async Task Una_reserva_de_otra_empresa_no_se_encuentra_y_queda_intacta()
    {
        (Empresa a, Empresa b) = await CrearEmpresasAsync();
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        Servicio servicio = DominioPrueba.Servicio(b.Id);
        Profesional profesional = DominioPrueba.Profesional(b.Id);
        Reserva reserva = Reserva.Crear(
            Guid.CreateVersion7(), b.Id, usuario.Id, profesional.Id, servicio, DominioPrueba.Ahora.AddDays(1),
            new NombrePersona("Lucia Fernandez"), new Telefono("699887766"), null, DominioPrueba.Ahora);
        await GuardarAsync(servicio, profesional, reserva);

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();
        RepositorioReservas desdeA = new(db, new ContextoEmpresaFijo(a.Id));
        RepositorioReservas desdeB = new(db, new ContextoEmpresaFijo(b.Id));

        (await desdeA.ObtenerPorIdAsync(reserva.Id, CancellationToken.None)).Should().BeNull();
        (await desdeA.ObtenerParaModificarAsync(reserva.Id, CancellationToken.None)).Should().BeNull();
        (await desdeB.ObtenerPorIdAsync(reserva.Id, CancellationToken.None)).Should().BeEquivalentTo(reserva);
    }

    private async Task<(Empresa A, Empresa B)> CrearEmpresasAsync()
    {
        string sufijo = DominioPrueba.Sufijo();
        Categoria categoria = DominioPrueba.Categoria(sufijo);
        Empresa a = DominioPrueba.Empresa($"{sufijo}-a", categoria.Id);
        Empresa b = DominioPrueba.Empresa($"{sufijo}-b", categoria.Id);

        await GuardarAsync(categoria, a, b);
        return (a, b);
    }

    private async Task GuardarAsync(params object[] entidades)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();
        db.AddRange(entidades);
        await db.SaveChangesAsync();
    }

    private async Task EjecutarSqlAsync(FormattableString sql)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        await ambito.ServiceProvider.GetRequiredService<ContextoDatos>().Database.ExecuteSqlInterpolatedAsync(sql);
    }
}
