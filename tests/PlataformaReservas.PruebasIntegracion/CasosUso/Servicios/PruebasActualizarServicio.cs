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
public sealed class PruebasActualizarServicio(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Actualiza_datos_y_politicas()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, servicio);

        Resultado resultado = await ActualizarAsync(empresa.Id, Comando(servicio.Id));

        Servicio leido = await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Servicios.SingleAsync(s => s.Id == servicio.Id));
        resultado.EsExito.Should().BeTrue();
        leido.Nombre.Should().Be(new NombreServicio("Corte clasico"));
        leido.Duracion.Minutos.Should().Be(40);
        leido.AntelacionMaximaDias.Should().Be(30);
    }

    [Fact]
    public async Task Un_servicio_de_otra_empresa_no_se_encuentra_y_queda_intacto()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Servicio deLaAjena = DominioPrueba.Servicio(ajena.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, deLaAjena);

        Resultado resultado = await ActualizarAsync(propia.Id, Comando(deLaAjena.Id));

        resultado.Codigo.Should().Be(CodigoError.NoEncontrado);
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Servicios.SingleAsync(s => s.Id == deLaAjena.Id)))
            .Nombre.Should().Be(new NombreServicio("Corte y barba"));
    }

    private static ActualizarServicioComando Comando(Guid id)
    {
        return new ActualizarServicioComando(id, "Corte clasico", null, 40, 20m, null, null, 30);
    }

    private async Task<Resultado> ActualizarAsync(Guid empresaId, ActualizarServicioComando comando)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        return await ambito.ServiceProvider.GetRequiredService<ActualizarServicio>().EjecutarAsync(comando, CancellationToken.None);
    }
}
