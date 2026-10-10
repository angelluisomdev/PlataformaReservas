using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Persistencia.Configuraciones;

[Collection("BaseDatos")]
public sealed class PruebasConfiguracionHorario(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Un_horario_se_relee_con_su_id_su_dia_y_sus_horas()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        Guid horarioId = Guid.CreateVersion7();
        IntervaloHorario intervalo = new(new TimeOnly(9, 30), new TimeOnly(13, 45));
        profesional.AgregarIntervalo(horarioId, DayOfWeek.Saturday, intervalo);
        await EscenarioEmpresa.GuardarAsync(baseDatos, profesional);

        Horario leido = await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Set<Horario>().SingleAsync(h => h.ProfesionalId == profesional.Id));

        leido.Id.Should().Be(horarioId);
        leido.EmpresaId.Should().Be(empresa.Id);
        leido.DiaSemana.Should().Be(DayOfWeek.Saturday);
        leido.Intervalo.Should().Be(intervalo);
    }
}
