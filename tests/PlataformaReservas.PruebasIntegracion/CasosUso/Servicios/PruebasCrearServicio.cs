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
public sealed class PruebasCrearServicio(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Crea_el_servicio_en_la_empresa_actual_con_las_politicas_indicadas()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);

        Resultado<Guid> resultado = await CrearAsync(empresa.Id, new CrearServicioComando("Corte", "Con lavado", 30, 15m, 20, null, null));

        Servicio leido = await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Servicios.SingleAsync(s => s.Id == resultado.Valor));
        leido.EmpresaId.Should().Be(empresa.Id);
        leido.Descripcion.Should().Be(new DescripcionServicio("Con lavado"));
        leido.IntervaloHuecosMinutos.Should().Be(20);
        leido.AntelacionMinimaMinutos.Should().BeNull();
        leido.AntelacionMaximaDias.Should().BeNull();
        leido.Activo.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 15, null)]
    [InlineData(30, -1, null)]
    [InlineData(30, 15, 0)]
    public async Task Un_dato_no_valido_da_validacion(int duracion, int precio, int? intervalo)
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);

        Resultado<Guid> resultado = await CrearAsync(empresa.Id, new CrearServicioComando("Corte", null, duracion, precio, intervalo, null, null));

        resultado.Codigo.Should().Be(CodigoError.Validacion);
    }

    private async Task<Resultado<Guid>> CrearAsync(Guid empresaId, CrearServicioComando comando)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        return await ambito.ServiceProvider.GetRequiredService<CrearServicio>().EjecutarAsync(comando, CancellationToken.None);
    }
}
