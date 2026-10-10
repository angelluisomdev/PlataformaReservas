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
public sealed class PruebasEliminarIntervalo(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Borra_la_fila_y_no_toca_las_reservas_ya_creadas()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        Guid horarioId = Guid.CreateVersion7();
        profesional.AgregarIntervalo(horarioId, DayOfWeek.Monday, new IntervaloHorario(new TimeOnly(9, 0), new TimeOnly(14, 0)));
        Reserva reserva = Reserva.Crear(
            Guid.CreateVersion7(), empresa.Id, usuario.Id, profesional.Id, servicio, DominioPrueba.Ahora.AddDays(2),
            new NombrePersona("Lucia Fernandez"), new Telefono("699887766"), null, DominioPrueba.Ahora);
        await EscenarioEmpresa.GuardarAsync(baseDatos, servicio, profesional, reserva);

        Resultado resultado = await EliminarAsync(empresa.Id, profesional.Id, horarioId);

        resultado.EsExito.Should().BeTrue();
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Set<Horario>().AnyAsync(h => h.Id == horarioId))).Should().BeFalse();
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Reservas.Where(r => r.Id == reserva.Id).Select(r => r.Estado).SingleAsync()))
            .Should().Be(EstadoReserva.Confirmada);
    }

    [Fact]
    public async Task Un_intervalo_que_no_existe_o_de_otra_empresa_no_se_encuentra()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional propio = DominioPrueba.Profesional(propia.Id);
        Profesional ajeno = DominioPrueba.Profesional(ajena.Id);
        Guid horarioAjeno = Guid.CreateVersion7();
        ajeno.AgregarIntervalo(horarioAjeno, DayOfWeek.Monday, new IntervaloHorario(new TimeOnly(9, 0), new TimeOnly(14, 0)));
        await EscenarioEmpresa.GuardarAsync(baseDatos, propio, ajeno);

        Resultado inexistente = await EliminarAsync(propia.Id, propio.Id, Guid.CreateVersion7());
        Resultado deOtra = await EliminarAsync(propia.Id, ajeno.Id, horarioAjeno);

        inexistente.Codigo.Should().Be(CodigoError.NoEncontrado);
        deOtra.Codigo.Should().Be(CodigoError.NoEncontrado);
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Set<Horario>().AnyAsync(h => h.Id == horarioAjeno))).Should().BeTrue();
    }

    [Fact]
    public async Task Con_dos_intervalos_borra_solo_el_indicado()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        Guid manana = Guid.CreateVersion7();
        Guid tarde = Guid.CreateVersion7();
        profesional.AgregarIntervalo(manana, DayOfWeek.Monday, new IntervaloHorario(new TimeOnly(9, 0), new TimeOnly(14, 0)));
        profesional.AgregarIntervalo(tarde, DayOfWeek.Monday, new IntervaloHorario(new TimeOnly(16, 0), new TimeOnly(20, 0)));
        await EscenarioEmpresa.GuardarAsync(baseDatos, profesional);

        Resultado resultado = await EliminarAsync(empresa.Id, profesional.Id, manana);

        resultado.EsExito.Should().BeTrue();
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Set<Horario>().Where(h => h.ProfesionalId == profesional.Id).Select(h => h.Id).ToListAsync()))
            .Should().Equal(tarde);
    }

    [Fact]
    public async Task Un_horario_de_otro_profesional_de_la_misma_empresa_no_se_encuentra_y_queda_intacto()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional ana = DominioPrueba.Profesional(empresa.Id);
        Profesional luis = DominioPrueba.Profesional(empresa.Id);
        Guid horarioDeLuis = Guid.CreateVersion7();
        luis.AgregarIntervalo(horarioDeLuis, DayOfWeek.Monday, new IntervaloHorario(new TimeOnly(9, 0), new TimeOnly(14, 0)));
        await EscenarioEmpresa.GuardarAsync(baseDatos, ana, luis);

        Resultado resultado = await EliminarAsync(empresa.Id, ana.Id, horarioDeLuis);

        resultado.Codigo.Should().Be(CodigoError.NoEncontrado);
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Set<Horario>().AnyAsync(h => h.Id == horarioDeLuis))).Should().BeTrue();
    }

    private async Task<Resultado> EliminarAsync(Guid empresaId, Guid profesionalId, Guid horarioId)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        return await ambito.ServiceProvider.GetRequiredService<EliminarIntervalo>()
            .EjecutarAsync(new EliminarIntervaloComando(profesionalId, horarioId), CancellationToken.None);
    }
}
