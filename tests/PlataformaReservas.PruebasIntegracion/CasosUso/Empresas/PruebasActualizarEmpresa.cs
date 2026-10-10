using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.PruebasIntegracion.Infraestructura;
using PlataformaReservas.Aplicacion.CasosUso.Empresas;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Empresas;

[Collection("BaseDatos")]
public sealed class PruebasActualizarEmpresa(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Actualiza_datos_categoria_y_politicas()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Categoria otra = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);

        Resultado resultado = await ActualizarAsync(empresa.Id, Comando(otra.Id, intervalo: 30));

        Empresa leida = await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Empresas.SingleAsync(e => e.Id == empresa.Id));
        resultado.EsExito.Should().BeTrue();
        leida.Nombre.Should().Be(new NombreEmpresa("Barberia Norte"));
        leida.CategoriaId.Should().Be(otra.Id);
        leida.Politicas.Should().Be(new PoliticasReserva(30, 0, 90));
        leida.FechaModificacion.Should().Be(DominioPrueba.Ahora);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(481)]
    public async Task Un_intervalo_fuera_de_rango_da_validacion_y_no_cambia_nada(int intervalo)
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);

        Resultado resultado = await ActualizarAsync(empresa.Id, Comando(empresa.CategoriaId, intervalo));

        Empresa leida = await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Empresas.SingleAsync(e => e.Id == empresa.Id));
        resultado.Codigo.Should().Be(CodigoError.Validacion);
        leida.Politicas.Should().Be(new PoliticasReserva(15, 60, 60));
        leida.Nombre.Should().Be(new NombreEmpresa("Barberia Centro"));
    }

    private static ActualizarEmpresaComando Comando(Guid categoriaId, int intervalo)
    {
        return new ActualizarEmpresaComando(
            "Barberia Norte", categoriaId, "Cortes", "norte@barberia.es", "611111111",
            "Calle Norte 2", "37002", "Salamanca", intervalo, 0, 90);
    }

    private async Task<Resultado> ActualizarAsync(Guid empresaId, ActualizarEmpresaComando comando)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        return await ambito.ServiceProvider.GetRequiredService<ActualizarEmpresa>().EjecutarAsync(comando, CancellationToken.None);
    }
}
