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
public sealed class PruebasDesasociarServicio(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Desasocia_un_servicio_que_presta()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        profesional.AsociarServicio(servicio.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, servicio, profesional);

        Resultado resultado = await DesasociarAsync(empresa.Id, profesional.Id, servicio.Id);

        Profesional leido = await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Profesionales.SingleAsync(p => p.Id == profesional.Id));
        resultado.EsExito.Should().BeTrue();
        leido.ServiciosQuePresta.Should().BeEmpty();
    }

    [Fact]
    public async Task Una_asociacion_que_no_existe_no_se_encuentra()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, profesional);

        Resultado resultado = await DesasociarAsync(empresa.Id, profesional.Id, Guid.CreateVersion7());

        resultado.Codigo.Should().Be(CodigoError.NoEncontrado);
    }

    private async Task<Resultado> DesasociarAsync(Guid empresaId, Guid profesionalId, Guid servicioId)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        return await ambito.ServiceProvider.GetRequiredService<DesasociarServicio>()
            .EjecutarAsync(new DesasociarServicioComando(profesionalId, servicioId), CancellationToken.None);
    }
}
