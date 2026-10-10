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
public sealed class PruebasAsociarServicio(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Asocia_un_servicio_de_la_misma_empresa()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, servicio, profesional);

        Resultado resultado = await AsociarAsync(empresa.Id, profesional.Id, servicio.Id);

        resultado.EsExito.Should().BeTrue();
        (await ServiciosQuePrestaAsync(profesional.Id)).Should().Equal(servicio.Id);
    }

    [Fact]
    public async Task Un_servicio_de_otra_empresa_no_se_encuentra_y_no_se_asocia()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Servicio deLaAjena = DominioPrueba.Servicio(ajena.Id);
        Profesional profesional = DominioPrueba.Profesional(propia.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, deLaAjena, profesional);

        Resultado resultado = await AsociarAsync(propia.Id, profesional.Id, deLaAjena.Id);

        resultado.Codigo.Should().Be(CodigoError.NoEncontrado);
        (await ServiciosQuePrestaAsync(profesional.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Asociar_dos_veces_da_conflicto()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, servicio, profesional);
        await AsociarAsync(empresa.Id, profesional.Id, servicio.Id);

        Resultado segunda = await AsociarAsync(empresa.Id, profesional.Id, servicio.Id);

        segunda.Codigo.Should().Be(CodigoError.Conflicto);
    }

    private async Task<Resultado> AsociarAsync(Guid empresaId, Guid profesionalId, Guid servicioId)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        return await ambito.ServiceProvider.GetRequiredService<AsociarServicio>()
            .EjecutarAsync(new AsociarServicioComando(profesionalId, servicioId), CancellationToken.None);
    }

    private async Task<IReadOnlySet<Guid>> ServiciosQuePrestaAsync(Guid profesionalId)
    {
        Profesional leido = await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Profesionales.SingleAsync(p => p.Id == profesionalId));
        return leido.ServiciosQuePresta;
    }
}
