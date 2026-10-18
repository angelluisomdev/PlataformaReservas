using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Reservas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Consultas;

[Collection("BaseDatos")]
public sealed class PruebasConsultasReservasCliente(BaseDatosFixture baseDatos)
{
    private static readonly DateTime Ahora = DominioPrueba.Ahora;

    [Fact]
    public async Task Solo_devuelve_las_reservas_del_usuario_de_todas_sus_empresas()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        EscenarioReserva otraEmpresa = await EscenarioReserva.CrearAsync(baseDatos);
        Usuario otro = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        Reserva enUna = escenario.Reserva(Ahora.AddDays(1));
        Reserva enOtra = otraEmpresa.Reserva(Ahora.AddDays(2), escenario.Cliente.Id);
        Reserva ajena = escenario.Reserva(Ahora.AddDays(3), otro.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, enUna, enOtra, ajena);

        MisReservasDto mias = await MisReservasAsync(escenario.Cliente.Id);

        mias.Proximas.Select(r => r.Id).Should().Equal(enUna.Id, enOtra.Id);
        mias.Historicas.Should().BeEmpty();
        mias.Proximas[0].Establecimiento.Should().Be(escenario.Empresa.Nombre.Valor);
        mias.Proximas[0].Servicio.Should().Be(escenario.Servicio.Nombre.Valor);
        mias.Proximas[0].Profesional.Should().Be(escenario.Profesional.NombreCompleto.Valor);
    }

    [Fact]
    public async Task Las_proximas_empiezan_en_ahora_y_las_historicas_van_de_la_mas_reciente_a_la_mas_antigua()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva anteayer = escenario.Reserva(Ahora.AddDays(-2));
        Reserva ayer = escenario.Reserva(Ahora.AddDays(-1));
        Reserva justoAhora = escenario.Reserva(Ahora);
        Reserva canceladaFutura = escenario.Reserva(Ahora.AddDays(1));
        canceladaFutura.CancelarPorCliente(Ahora, OrigenCancelacion.Cliente);
        await EscenarioEmpresa.GuardarAsync(baseDatos, anteayer, ayer, justoAhora, canceladaFutura);
        await EjecutarSqlAsync($"UPDATE reservas SET estado = {(int)EstadoReserva.Completada} WHERE id = {anteayer.Id}");

        MisReservasDto mias = await MisReservasAsync(escenario.Cliente.Id);

        mias.Proximas.Select(r => r.Id).Should().Equal(justoAhora.Id, canceladaFutura.Id);
        mias.Proximas[1].Estado.Should().Be(EstadoReserva.Cancelada);
        mias.Historicas.Select(r => r.Id).Should().Equal(ayer.Id, anteayer.Id);
        mias.Historicas[1].Estado.Should().Be(EstadoReserva.Completada);
    }

    [Fact]
    public async Task El_detalle_de_una_reserva_ajena_o_inexistente_es_nulo()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Usuario otro = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        Reserva ajena = escenario.Reserva(Ahora.AddDays(1), otro.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, ajena);

        (await DetalleAsync(escenario.Cliente.Id, ajena.Id)).Should().BeNull();
        (await DetalleAsync(escenario.Cliente.Id, Guid.CreateVersion7())).Should().BeNull();
    }

    [Fact]
    public async Task El_detalle_lleva_el_precio_aplicado_aunque_el_servicio_cambie_y_dice_si_es_cancelable()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva futura = escenario.Reserva(Ahora.AddDays(1));
        Reserva iniciada = escenario.Reserva(Ahora.AddHours(-1));
        await EscenarioEmpresa.GuardarAsync(baseDatos, futura, iniciada);
        await EjecutarSqlAsync($"UPDATE servicios SET precio = 99 WHERE id = {escenario.Servicio.Id}");

        DetalleReservaClienteDto detalle = (await DetalleAsync(escenario.Cliente.Id, futura.Id))!;

        detalle.PrecioAplicado.Should().Be(18.50m);
        detalle.DuracionMinutos.Should().Be(45);
        detalle.FinUtc.Should().Be(futura.Franja.FinUtc);
        detalle.Cancelable.Should().BeTrue();
        (await DetalleAsync(escenario.Cliente.Id, iniciada.Id))!.Cancelable.Should().BeFalse();
    }

    [Fact]
    public async Task Las_consultas_no_dejan_entidades_en_seguimiento()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva reserva = escenario.Reserva(Ahora.AddDays(1));
        await EscenarioEmpresa.GuardarAsync(baseDatos, reserva);

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(escenario.Cliente.Id);
        IConsultasReservasCliente consultas = ambito.ServiceProvider.GetRequiredService<IConsultasReservasCliente>();
        await consultas.ObtenerMisReservasAsync(Ahora, CancellationToken.None);
        await consultas.ObtenerDetalleAsync(reserva.Id, Ahora, CancellationToken.None);

        ambito.ServiceProvider.GetRequiredService<ContextoDatos>().ChangeTracker.Entries().Should().BeEmpty();
    }

    private async Task<MisReservasDto> MisReservasAsync(Guid usuarioId)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(usuarioId);
        return await ambito.ServiceProvider.GetRequiredService<IConsultasReservasCliente>()
            .ObtenerMisReservasAsync(Ahora, CancellationToken.None);
    }

    private async Task<DetalleReservaClienteDto?> DetalleAsync(Guid usuarioId, Guid reservaId)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(usuarioId);
        return await ambito.ServiceProvider.GetRequiredService<IConsultasReservasCliente>()
            .ObtenerDetalleAsync(reservaId, Ahora, CancellationToken.None);
    }

    private async Task EjecutarSqlAsync(FormattableString sql)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        await ambito.ServiceProvider.GetRequiredService<ContextoDatos>().Database.ExecuteSqlInterpolatedAsync(sql);
    }
}
