using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.CasosUso.Horarios;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Horarios;

[Collection("BaseDatos")]
public sealed class PruebasAgregarIntervalo(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Guarda_dos_intervalos_el_mismo_dia_como_altas()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, profesional);

        Resultado<Guid> manana = await AgregarAsync(empresa.Id, profesional.Id, new TimeOnly(9, 0), new TimeOnly(14, 0));
        Resultado<Guid> tarde = await AgregarAsync(empresa.Id, profesional.Id, new TimeOnly(16, 0), new TimeOnly(20, 0));

        manana.EsExito.Should().BeTrue();
        tarde.EsExito.Should().BeTrue();
        List<Horario> guardados = await EscenarioEmpresa.LeerAsync(
            baseDatos, db => db.Set<Horario>().Where(h => h.ProfesionalId == profesional.Id).ToListAsync());
        guardados.Select(h => h.Id).Should().BeEquivalentTo([manana.Valor, tarde.Valor]);
        guardados.Should().OnlyContain(h => h.EmpresaId == empresa.Id && h.DiaSemana == DayOfWeek.Monday);
    }

    [Fact]
    public async Task Un_intervalo_solapado_o_invertido_da_validacion_y_no_guarda()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Monday, new IntervaloHorario(new TimeOnly(9, 0), new TimeOnly(14, 0)));
        await EscenarioEmpresa.GuardarAsync(baseDatos, profesional);

        Resultado<Guid> solapado = await AgregarAsync(empresa.Id, profesional.Id, new TimeOnly(13, 0), new TimeOnly(15, 0));
        Resultado<Guid> invertido = await AgregarAsync(empresa.Id, profesional.Id, new TimeOnly(18, 0), new TimeOnly(17, 0));

        solapado.Codigo.Should().Be(CodigoError.Validacion);
        invertido.Codigo.Should().Be(CodigoError.Validacion);
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Set<Horario>().CountAsync(h => h.ProfesionalId == profesional.Id)))
            .Should().Be(1);
    }

    [Fact]
    public async Task Un_profesional_de_otra_empresa_no_se_encuentra()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional ajeno = DominioPrueba.Profesional(ajena.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, ajeno);

        Resultado<Guid> resultado = await AgregarAsync(propia.Id, ajeno.Id, new TimeOnly(9, 0), new TimeOnly(14, 0));

        resultado.Codigo.Should().Be(CodigoError.NoEncontrado);
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Set<Horario>().AnyAsync(h => h.ProfesionalId == ajeno.Id)))
            .Should().BeFalse();
    }

    [Fact]
    public async Task Un_profesional_inexistente_no_se_encuentra()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);

        Resultado<Guid> resultado = await AgregarAsync(empresa.Id, Guid.CreateVersion7(), new TimeOnly(9, 0), new TimeOnly(14, 0));

        resultado.Codigo.Should().Be(CodigoError.NoEncontrado);
    }

    private async Task<Resultado<Guid>> AgregarAsync(Guid empresaId, Guid profesionalId, TimeOnly inicio, TimeOnly fin)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        return await ambito.ServiceProvider.GetRequiredService<AgregarIntervalo>()
            .EjecutarAsync(new AgregarIntervaloComando(profesionalId, DayOfWeek.Monday, inicio, fin), CancellationToken.None);
    }
}
