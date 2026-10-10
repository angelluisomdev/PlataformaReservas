using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.CasosUso.Horarios;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Horarios;

[Collection("BaseDatos")]
public sealed class PruebasQuitarExcepcion(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Borra_la_fila_de_la_excepcion()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        DateOnly fecha = new(2026, 12, 25);
        profesional.MarcarNoDisponible(Guid.CreateVersion7(), fecha, null);
        await EscenarioEmpresa.GuardarAsync(baseDatos, profesional);

        Resultado resultado = await QuitarAsync(empresa.Id, profesional.Id, fecha);

        resultado.EsExito.Should().BeTrue();
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Set<ExcepcionHorario>().AnyAsync(x => x.ProfesionalId == profesional.Id)))
            .Should().BeFalse();
    }

    [Fact]
    public async Task Una_fecha_sin_excepcion_o_un_profesional_de_otra_empresa_no_se_encuentran()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional propio = DominioPrueba.Profesional(propia.Id);
        Profesional ajeno = DominioPrueba.Profesional(ajena.Id);
        DateOnly fecha = new(2026, 12, 25);
        ajeno.MarcarNoDisponible(Guid.CreateVersion7(), fecha, null);
        await EscenarioEmpresa.GuardarAsync(baseDatos, propio, ajeno);

        Resultado sinExcepcion = await QuitarAsync(propia.Id, propio.Id, fecha);
        Resultado deOtra = await QuitarAsync(propia.Id, ajeno.Id, fecha);

        sinExcepcion.Codigo.Should().Be(CodigoError.NoEncontrado);
        deOtra.Codigo.Should().Be(CodigoError.NoEncontrado);
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Set<ExcepcionHorario>().AnyAsync(x => x.ProfesionalId == ajeno.Id)))
            .Should().BeTrue();
    }

    [Fact]
    public async Task Con_dos_excepciones_quita_solo_la_de_esa_fecha()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        DateOnly nochebuena = new(2026, 12, 24);
        DateOnly navidad = new(2026, 12, 25);
        profesional.MarcarNoDisponible(Guid.CreateVersion7(), nochebuena, null);
        profesional.MarcarNoDisponible(Guid.CreateVersion7(), navidad, null);
        await EscenarioEmpresa.GuardarAsync(baseDatos, profesional);

        Resultado resultado = await QuitarAsync(empresa.Id, profesional.Id, nochebuena);

        resultado.EsExito.Should().BeTrue();
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Set<ExcepcionHorario>().Where(x => x.ProfesionalId == profesional.Id).Select(x => x.Fecha).ToListAsync()))
            .Should().Equal(navidad);
    }

    private async Task<Resultado> QuitarAsync(Guid empresaId, Guid profesionalId, DateOnly fecha)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        return await ambito.ServiceProvider.GetRequiredService<QuitarExcepcion>()
            .EjecutarAsync(new QuitarExcepcionComando(profesionalId, fecha), CancellationToken.None);
    }
}
