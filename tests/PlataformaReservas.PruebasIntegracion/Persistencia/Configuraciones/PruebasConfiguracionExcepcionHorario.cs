using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Persistencia.Configuraciones;

[Collection("BaseDatos")]
public sealed class PruebasConfiguracionExcepcionHorario(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Una_excepcion_con_motivo_y_otra_sin_motivo_se_releen_igual()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        DateOnly conMotivo = new(2026, 12, 24);
        DateOnly sinMotivo = new(2026, 12, 31);
        profesional.MarcarNoDisponible(Guid.CreateVersion7(), conMotivo, new MotivoExcepcion("Nochebuena"));
        profesional.MarcarNoDisponible(Guid.CreateVersion7(), sinMotivo, null);
        await EscenarioEmpresa.GuardarAsync(baseDatos, profesional);

        List<ExcepcionHorario> leidas = await EscenarioEmpresa.LeerAsync(
            baseDatos, db => db.Set<ExcepcionHorario>().Where(x => x.ProfesionalId == profesional.Id).OrderBy(x => x.Fecha).ToListAsync());

        leidas.Should().HaveCount(2);
        leidas[0].Fecha.Should().Be(conMotivo);
        leidas[0].Motivo.Should().Be(new MotivoExcepcion("Nochebuena"));
        leidas[1].Fecha.Should().Be(sinMotivo);
        leidas[1].Motivo.Should().BeNull();
    }
}
