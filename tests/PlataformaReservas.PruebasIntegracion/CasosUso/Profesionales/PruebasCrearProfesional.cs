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
public sealed class PruebasCrearProfesional(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Crea_el_profesional_en_la_empresa_actual_con_sus_datos_de_contacto()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);

        Resultado<Guid> resultado = await CrearAsync(empresa.Id, new CrearProfesionalComando("Ana Garcia", "ana@barberia.es", "600111222"));

        Profesional leido = await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Profesionales.SingleAsync(p => p.Id == resultado.Valor));
        leido.EmpresaId.Should().Be(empresa.Id);
        leido.Email.Should().Be(new Email("ana@barberia.es"));
        leido.Telefono.Should().Be(new Telefono("600111222"));
        leido.Activo.Should().BeTrue();
    }

    [Fact]
    public async Task Un_telefono_mal_formado_da_validacion()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);

        Resultado<Guid> resultado = await CrearAsync(empresa.Id, new CrearProfesionalComando("Ana Garcia", null, "600 abc"));

        resultado.Codigo.Should().Be(CodigoError.Validacion);
    }

    private async Task<Resultado<Guid>> CrearAsync(Guid empresaId, CrearProfesionalComando comando)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        return await ambito.ServiceProvider.GetRequiredService<CrearProfesional>().EjecutarAsync(comando, CancellationToken.None);
    }
}
