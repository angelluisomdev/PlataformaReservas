using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.PruebasIntegracion.Infraestructura;
using PlataformaReservas.Aplicacion.CasosUso.Profesionales;
using PlataformaReservas.Aplicacion.Dtos;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Profesionales;

[Collection("BaseDatos")]
public sealed class PruebasDesactivarProfesional(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Desactiva_sin_borrar_y_una_segunda_vez_da_estado_no_valido()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, profesional);

        Resultado primera = await DesactivarAsync(empresa.Id, profesional.Id);
        Resultado segunda = await DesactivarAsync(empresa.Id, profesional.Id);

        primera.EsExito.Should().BeTrue();
        segunda.Codigo.Should().Be(CodigoError.EstadoNoValido);
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Profesionales.SingleAsync(p => p.Id == profesional.Id)))
            .Activo.Should().BeFalse();
    }

    [Fact]
    public async Task Un_profesional_de_otra_empresa_no_se_encuentra_y_sigue_activo()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional deLaAjena = DominioPrueba.Profesional(ajena.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, deLaAjena);

        Resultado resultado = await DesactivarAsync(propia.Id, deLaAjena.Id);

        resultado.Codigo.Should().Be(CodigoError.NoEncontrado);
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Profesionales.SingleAsync(p => p.Id == deLaAjena.Id)))
            .Activo.Should().BeTrue();
    }

    private async Task<Resultado> DesactivarAsync(Guid empresaId, Guid profesionalId)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        return await ambito.ServiceProvider.GetRequiredService<DesactivarProfesional>()
            .EjecutarAsync(new DesactivarProfesionalComando(profesionalId), CancellationToken.None);
    }
}
