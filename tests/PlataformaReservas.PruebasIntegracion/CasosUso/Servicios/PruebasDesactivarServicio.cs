using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.PruebasIntegracion.Infraestructura;
using PlataformaReservas.Aplicacion.CasosUso.Servicios;
using PlataformaReservas.Aplicacion.Dtos;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Servicios;

[Collection("BaseDatos")]
public sealed class PruebasDesactivarServicio(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Desactiva_sin_borrar_y_una_segunda_vez_da_estado_no_valido()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, servicio);

        Resultado primera = await DesactivarAsync(empresa.Id, servicio.Id);
        Resultado segunda = await DesactivarAsync(empresa.Id, servicio.Id);

        primera.EsExito.Should().BeTrue();
        segunda.Codigo.Should().Be(CodigoError.EstadoNoValido);
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Servicios.SingleAsync(s => s.Id == servicio.Id))).Activo.Should().BeFalse();
    }

    [Fact]
    public async Task Un_servicio_de_otra_empresa_no_se_encuentra_y_sigue_activo()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Servicio deLaAjena = DominioPrueba.Servicio(ajena.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, deLaAjena);

        Resultado resultado = await DesactivarAsync(propia.Id, deLaAjena.Id);

        resultado.Codigo.Should().Be(CodigoError.NoEncontrado);
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Servicios.SingleAsync(s => s.Id == deLaAjena.Id))).Activo.Should().BeTrue();
    }

    private async Task<Resultado> DesactivarAsync(Guid empresaId, Guid servicioId)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        return await ambito.ServiceProvider.GetRequiredService<DesactivarServicio>()
            .EjecutarAsync(new DesactivarServicioComando(servicioId), CancellationToken.None);
    }
}
