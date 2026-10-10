using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.PruebasIntegracion.Infraestructura;
using PlataformaReservas.Aplicacion.CasosUso.Empresas;
using PlataformaReservas.Aplicacion.Dtos;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Empresas;

[Collection("BaseDatos")]
public sealed class PruebasObtenerConfiguracionEmpresa(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Devuelve_los_datos_las_politicas_y_el_recuento_de_reservas_futuras()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        Reserva futura = Reserva.Crear(
            Guid.CreateVersion7(), empresa.Id, usuario.Id, profesional.Id, servicio, DominioPrueba.Ahora.AddDays(1),
            new NombrePersona("Lucia"), new Telefono("699887766"), null, DominioPrueba.Ahora);
        await EscenarioEmpresa.GuardarAsync(baseDatos, servicio, profesional, futura);

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresa.Id);
        Resultado<ConfiguracionEmpresaDto> resultado = await ambito.ServiceProvider
            .GetRequiredService<ObtenerConfiguracionEmpresa>().EjecutarAsync(CancellationToken.None);

        resultado.EsExito.Should().BeTrue();
        resultado.Valor!.Nombre.Should().Be("Barberia Centro");
        resultado.Valor.Slug.Should().Be(empresa.Slug.Valor);
        resultado.Valor.IntervaloHuecosMinutos.Should().Be(15);
        resultado.Valor.Activa.Should().BeTrue();
        resultado.Valor.ReservasFuturas.Should().Be(1);
    }
}
