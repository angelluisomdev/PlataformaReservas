using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.CasosUso.Horarios;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Horarios;

[Collection("BaseDatos")]
public sealed class PruebasMarcarNoDisponible(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Guarda_la_excepcion_con_su_motivo_y_no_cancela_las_reservas_de_ese_dia()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        DateTime inicio = DominioPrueba.Ahora.AddDays(2);
        Reserva reserva = Reserva.Crear(
            Guid.CreateVersion7(), empresa.Id, usuario.Id, profesional.Id, servicio, inicio,
            new NombrePersona("Lucia Fernandez"), new Telefono("699887766"), null, DominioPrueba.Ahora);
        await EscenarioEmpresa.GuardarAsync(baseDatos, servicio, profesional, reserva);
        DateOnly fecha = DateOnly.FromDateTime(inicio);

        Resultado resultado = await MarcarAsync(empresa.Id, profesional.Id, fecha, "  Vacaciones  ");

        resultado.EsExito.Should().BeTrue();
        ExcepcionHorario excepcion = await EscenarioEmpresa.LeerAsync(
            baseDatos, db => db.Set<ExcepcionHorario>().SingleAsync(x => x.ProfesionalId == profesional.Id));
        excepcion.Fecha.Should().Be(fecha);
        excepcion.Motivo.Should().Be(new MotivoExcepcion("Vacaciones"));
        excepcion.EmpresaId.Should().Be(empresa.Id);
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Reservas.Where(r => r.Id == reserva.Id).Select(r => r.Estado).SingleAsync()))
            .Should().Be(EstadoReserva.Confirmada);
    }

    [Fact]
    public async Task Una_segunda_excepcion_el_mismo_dia_o_un_motivo_demasiado_largo_dan_validacion()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, profesional);
        DateOnly fecha = new(2026, 12, 25);
        await MarcarAsync(empresa.Id, profesional.Id, fecha, null);

        Resultado segunda = await MarcarAsync(empresa.Id, profesional.Id, fecha, "Otra");
        Resultado larga = await MarcarAsync(empresa.Id, profesional.Id, fecha.AddDays(1), new string('x', MotivoExcepcion.LongitudMaxima + 1));

        segunda.Codigo.Should().Be(CodigoError.Validacion);
        larga.Codigo.Should().Be(CodigoError.Validacion);
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Set<ExcepcionHorario>().CountAsync(x => x.ProfesionalId == profesional.Id)))
            .Should().Be(1);
    }

    [Fact]
    public async Task Un_profesional_de_otra_empresa_no_se_encuentra()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional ajeno = DominioPrueba.Profesional(ajena.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, ajeno);

        Resultado resultado = await MarcarAsync(propia.Id, ajeno.Id, new DateOnly(2026, 12, 25), null);

        resultado.Codigo.Should().Be(CodigoError.NoEncontrado);
    }

    [Fact]
    public async Task Un_motivo_en_blanco_se_guarda_como_sin_motivo()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, profesional);

        Resultado resultado = await MarcarAsync(empresa.Id, profesional.Id, new DateOnly(2026, 12, 25), "   ");

        resultado.EsExito.Should().BeTrue();
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Set<ExcepcionHorario>().SingleAsync(x => x.ProfesionalId == profesional.Id)))
            .Motivo.Should().BeNull();
    }

    [Fact]
    public async Task Un_profesional_inexistente_no_se_encuentra()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);

        Resultado resultado = await MarcarAsync(empresa.Id, Guid.CreateVersion7(), new DateOnly(2026, 12, 25), null);

        resultado.Codigo.Should().Be(CodigoError.NoEncontrado);
    }

    private async Task<Resultado> MarcarAsync(Guid empresaId, Guid profesionalId, DateOnly fecha, string? motivo)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        return await ambito.ServiceProvider.GetRequiredService<MarcarNoDisponible>()
            .EjecutarAsync(new MarcarNoDisponibleComando(profesionalId, fecha, motivo), CancellationToken.None);
    }
}
