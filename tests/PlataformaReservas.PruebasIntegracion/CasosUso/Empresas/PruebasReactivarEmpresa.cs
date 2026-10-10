using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.PruebasIntegracion.Infraestructura;
using PlataformaReservas.Aplicacion.CasosUso.Empresas;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Empresas;

[Collection("BaseDatos")]
public sealed class PruebasReactivarEmpresa(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Reactiva_la_empresa_sin_restaurar_las_reservas_canceladas()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        Reserva futura = Reserva.Crear(
            Guid.CreateVersion7(), empresa.Id, usuario.Id, profesional.Id, servicio, DominioPrueba.Ahora.AddDays(1),
            new NombrePersona("Lucia"), new Telefono("699887766"), null, DominioPrueba.Ahora);
        await EscenarioEmpresa.GuardarAsync(baseDatos, servicio, profesional, futura);
        await using (AsyncServiceScope baja = baseDatos.AmbitoDeEmpresa(empresa.Id))
        {
            await baja.ServiceProvider.GetRequiredService<DarDeBajaEmpresa>().EjecutarAsync(CancellationToken.None);
        }

        Resultado resultado = await ReactivarAsync(empresa.Id);

        resultado.EsExito.Should().BeTrue();
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Empresas.SingleAsync(e => e.Id == empresa.Id))).Activa.Should().BeTrue();
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Reservas.Where(r => r.Id == futura.Id).Select(r => r.Estado).SingleAsync()))
            .Should().Be(EstadoReserva.Cancelada);
    }

    [Fact]
    public async Task Una_empresa_activa_da_estado_no_valido()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);

        Resultado resultado = await ReactivarAsync(empresa.Id);

        resultado.Codigo.Should().Be(CodigoError.EstadoNoValido);
    }

    private async Task<Resultado> ReactivarAsync(Guid empresaId)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        return await ambito.ServiceProvider.GetRequiredService<ReactivarEmpresa>().EjecutarAsync(CancellationToken.None);
    }
}
