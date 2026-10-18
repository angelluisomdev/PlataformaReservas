using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.CasosUso.Reservas;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos.Reservas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Reservas;

[Collection("BaseDatos")]
public sealed class PruebasConsultarMisReservas(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Devuelve_las_reservas_del_usuario_separadas_por_el_reloj_del_sistema()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva futura = escenario.Reserva(DominioPrueba.Ahora.AddDays(1));
        Reserva pasada = escenario.Reserva(DominioPrueba.Ahora.AddDays(-1));
        await EscenarioEmpresa.GuardarAsync(baseDatos, futura, pasada);

        Resultado<MisReservasDto> resultado = await ConsultarAsync(escenario.Cliente.Id);

        resultado.EsExito.Should().BeTrue();
        resultado.Valor!.Proximas.Should().ContainSingle().Which.Id.Should().Be(futura.Id);
        resultado.Valor.Historicas.Should().ContainSingle().Which.Id.Should().Be(pasada.Id);
    }

    [Fact]
    public async Task Cuenta_como_cancelables_las_confirmadas_futuras_igual_que_la_baja()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva futura = escenario.Reserva(DominioPrueba.Ahora.AddDays(1));
        Reserva cancelada = escenario.Reserva(DominioPrueba.Ahora.AddDays(2));
        cancelada.CancelarPorCliente(DominioPrueba.Ahora, OrigenCancelacion.Cliente);
        await EscenarioEmpresa.GuardarAsync(baseDatos, futura, cancelada);

        Resultado<MisReservasDto> resultado = await ConsultarAsync(escenario.Cliente.Id);

        resultado.Valor!.Cancelables.Should().Be(1);
    }

    [Fact]
    public async Task Un_usuario_de_la_sesion_sin_reservas_o_inexistente_ve_las_dos_pestanas_vacias()
    {
        Resultado<MisReservasDto> resultado = await ConsultarAsync(Guid.CreateVersion7());

        resultado.EsExito.Should().BeTrue();
        resultado.Valor!.Proximas.Should().BeEmpty();
        resultado.Valor.Historicas.Should().BeEmpty();
        resultado.Valor.Cancelables.Should().Be(0);
    }

    [Fact]
    public async Task Sin_sesion_da_no_autorizado()
    {
        (await ConsultarAsync(null)).Codigo.Should().Be(CodigoError.NoAutorizado);
    }

    private async Task<Resultado<MisReservasDto>> ConsultarAsync(Guid? usuarioId)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(usuarioId);
        return await ambito.ServiceProvider.GetRequiredService<ConsultarMisReservas>().EjecutarAsync(CancellationToken.None);
    }
}
