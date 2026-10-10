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
public sealed class PruebasListarProfesionales(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Lista_los_profesionales_de_la_empresa_con_los_servicios_que_prestan()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        profesional.AsociarServicio(servicio.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, servicio, profesional, DominioPrueba.Profesional(ajena.Id));

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresa.Id);
        Resultado<IReadOnlyList<ProfesionalDto>> resultado = await ambito.ServiceProvider
            .GetRequiredService<ListarProfesionales>().EjecutarAsync(CancellationToken.None);

        ProfesionalDto dto = resultado.Valor!.Should().ContainSingle().Subject;
        dto.Id.Should().Be(profesional.Id);
        dto.Servicios.Should().Equal(servicio.Id);
    }
}
