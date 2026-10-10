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
public sealed class PruebasActualizarProfesional(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Actualiza_nombre_y_contacto()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, profesional);

        Resultado resultado = await ActualizarAsync(empresa.Id, new ActualizarProfesionalComando(profesional.Id, "Ana Lopez", "ana@barberia.es", null));

        Profesional leido = await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Profesionales.SingleAsync(p => p.Id == profesional.Id));
        resultado.EsExito.Should().BeTrue();
        leido.NombreCompleto.Should().Be(new NombrePersona("Ana Lopez"));
        leido.Email.Should().Be(new Email("ana@barberia.es"));
    }

    [Fact]
    public async Task Un_profesional_de_otra_empresa_no_se_encuentra()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional deLaAjena = DominioPrueba.Profesional(ajena.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, deLaAjena);

        Resultado resultado = await ActualizarAsync(propia.Id, new ActualizarProfesionalComando(deLaAjena.Id, "Otro", null, null));

        resultado.Codigo.Should().Be(CodigoError.NoEncontrado);
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Profesionales.SingleAsync(p => p.Id == deLaAjena.Id)))
            .NombreCompleto.Should().Be(new NombrePersona("Ana Garcia"));
    }

    private async Task<Resultado> ActualizarAsync(Guid empresaId, ActualizarProfesionalComando comando)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        return await ambito.ServiceProvider.GetRequiredService<ActualizarProfesional>().EjecutarAsync(comando, CancellationToken.None);
    }
}
