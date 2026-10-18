using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.CasosUso.Reservas;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos.Reservas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Reservas;

[Collection("BaseDatos")]
public sealed class PruebasConsultarDetalleReserva(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Devuelve_el_detalle_de_una_reserva_propia()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva reserva = escenario.Reserva(DominioPrueba.Ahora.AddDays(1));
        await EscenarioEmpresa.GuardarAsync(baseDatos, reserva);

        Resultado<DetalleReservaClienteDto> resultado = await ConsultarAsync(escenario.Cliente.Id, reserva.Id);

        resultado.EsExito.Should().BeTrue();
        resultado.Valor!.Id.Should().Be(reserva.Id);
        resultado.Valor.Cancelable.Should().BeTrue();
    }

    [Fact]
    public async Task Una_reserva_de_otro_usuario_o_inexistente_da_no_encontrado()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Usuario otro = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        Reserva ajena = escenario.Reserva(DominioPrueba.Ahora.AddDays(1), otro.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, ajena);

        (await ConsultarAsync(escenario.Cliente.Id, ajena.Id)).Codigo.Should().Be(CodigoError.NoEncontrado);
        (await ConsultarAsync(escenario.Cliente.Id, Guid.CreateVersion7())).Codigo.Should().Be(CodigoError.NoEncontrado);
    }

    [Fact]
    public async Task Un_identificador_vacio_da_no_encontrado()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);

        (await ConsultarAsync(escenario.Cliente.Id, Guid.Empty)).Codigo.Should().Be(CodigoError.NoEncontrado);
    }

    [Fact]
    public async Task Sin_sesion_da_no_autorizado()
    {
        (await ConsultarAsync(null, Guid.CreateVersion7())).Codigo.Should().Be(CodigoError.NoAutorizado);
    }

    private async Task<Resultado<DetalleReservaClienteDto>> ConsultarAsync(Guid? usuarioId, Guid reservaId)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(usuarioId);
        return await ambito.ServiceProvider.GetRequiredService<ConsultarDetalleReserva>()
            .EjecutarAsync(new ConsultarDetalleReservaComando(reservaId), CancellationToken.None);
    }
}
