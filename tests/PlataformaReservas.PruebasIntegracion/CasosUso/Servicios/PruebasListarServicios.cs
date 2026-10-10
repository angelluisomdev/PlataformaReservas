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
public sealed class PruebasListarServicios(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Lista_los_servicios_de_la_empresa_ordenados_por_nombre()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Servicio corte = DominioPrueba.Servicio(empresa.Id);
        Servicio afeitado = Servicio.Crear(
            Guid.CreateVersion7(), empresa.Id, new NombreServicio("Afeitado"), new Duracion(20), new Precio(9m), DominioPrueba.Ahora);
        await EscenarioEmpresa.GuardarAsync(baseDatos, corte, afeitado, DominioPrueba.Servicio(ajena.Id));

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresa.Id);
        Resultado<IReadOnlyList<ServicioDto>> resultado = await ambito.ServiceProvider
            .GetRequiredService<ListarServicios>().EjecutarAsync(CancellationToken.None);

        IReadOnlyList<ServicioDto> servicios = resultado.Valor!;
        servicios.Select(s => s.Nombre).Should().Equal("Afeitado", "Corte y barba");
        servicios[0].DuracionMinutos.Should().Be(20);
    }
}
