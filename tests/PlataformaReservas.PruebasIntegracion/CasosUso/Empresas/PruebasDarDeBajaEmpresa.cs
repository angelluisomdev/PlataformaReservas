using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Eventos;
using PlataformaReservas.PruebasIntegracion.Infraestructura;
using Microsoft.AspNetCore.Identity;
using PlataformaReservas.Aplicacion.CasosUso.Empresas;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Empresas;

[Collection("BaseDatos")]
public sealed class PruebasDarDeBajaEmpresa(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Cancela_solo_las_reservas_futuras_confirmadas_y_deja_intactas_las_demas()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        Reserva futura1 = Nueva(empresa.Id, usuario.Id, profesional.Id, servicio, DominioPrueba.Ahora.AddDays(1));
        Reserva futura2 = Nueva(empresa.Id, usuario.Id, profesional.Id, servicio, DominioPrueba.Ahora.AddDays(2));
        Reserva pasada = Nueva(empresa.Id, usuario.Id, profesional.Id, servicio, DominioPrueba.Ahora.AddDays(-1));
        Reserva completada = Nueva(empresa.Id, usuario.Id, profesional.Id, servicio, DominioPrueba.Ahora.AddDays(-2));
        await EscenarioEmpresa.GuardarAsync(baseDatos, servicio, profesional, futura1, futura2, pasada, completada);
        await MarcarCompletadaAsync(completada.Id);

        Resultado<int> resultado = await DarDeBajaAsync(empresa.Id);

        resultado.EsExito.Should().BeTrue();
        resultado.Valor.Should().Be(2);
        (await EstadoAsync(futura1.Id)).Should().Be(EstadoReserva.Cancelada);
        (await EstadoAsync(futura2.Id)).Should().Be(EstadoReserva.Cancelada);
        (await EstadoAsync(pasada.Id)).Should().Be(EstadoReserva.Confirmada);
        (await EstadoAsync(completada.Id)).Should().Be(EstadoReserva.Completada);
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Empresas.SingleAsync(e => e.Id == empresa.Id))).Activa.Should().BeFalse();
    }

    [Fact]
    public async Task Renueva_el_sello_del_propietario()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Usuario propietario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        await EscenarioEmpresa.GuardarAsync(
            baseDatos, MiembroEmpresa.Crear(Guid.CreateVersion7(), empresa.Id, propietario.Id, RolMiembro.Propietario, DominioPrueba.Ahora));
        string? antes = await SelloAsync(propietario.Id);

        await DarDeBajaAsync(empresa.Id);

        (await SelloAsync(propietario.Id)).Should().NotBe(antes);
    }

    [Fact]
    public async Task Una_empresa_ya_dada_de_baja_da_estado_no_valido()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        await DarDeBajaAsync(empresa.Id);

        Resultado<int> otraVez = await DarDeBajaAsync(empresa.Id);

        otraVez.Codigo.Should().Be(CodigoError.EstadoNoValido);
    }

    private static Reserva Nueva(Guid empresaId, Guid usuarioId, Guid profesionalId, Servicio servicio, DateTime inicioUtc)
    {
        return Reserva.Crear(
            Guid.CreateVersion7(), empresaId, usuarioId, profesionalId, servicio, inicioUtc,
            new NombrePersona("Lucia Fernandez"), new Telefono("699887766"), null, DominioPrueba.Ahora);
    }

    private async Task<Resultado<int>> DarDeBajaAsync(Guid empresaId)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        return await ambito.ServiceProvider.GetRequiredService<DarDeBajaEmpresa>().EjecutarAsync(CancellationToken.None);
    }

    private async Task MarcarCompletadaAsync(Guid reservaId)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        await ambito.ServiceProvider.GetRequiredService<ContextoDatos>().Database
            .ExecuteSqlInterpolatedAsync($"UPDATE reservas SET estado = {(int)EstadoReserva.Completada} WHERE id = {reservaId}");
    }

    private Task<EstadoReserva> EstadoAsync(Guid reservaId)
    {
        return EscenarioEmpresa.LeerAsync(
            baseDatos, db => db.Reservas.Where(r => r.Id == reservaId).Select(r => r.Estado).SingleAsync());
    }

    private async Task<string?> SelloAsync(Guid usuarioId)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        return (await usuarios.FindByIdAsync(usuarioId.ToString()))!.SecurityStamp;
    }

    [Fact]
    public async Task Con_N_reservas_futuras_publica_la_baja_y_N_cancelaciones_por_baja_de_la_empresa()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        Reserva[] futuras =
        [
            .. Enumerable.Range(1, 3).Select(d => Nueva(empresa.Id, usuario.Id, profesional.Id, servicio, DominioPrueba.Ahora.AddDays(d))),
        ];
        Reserva pasada = Nueva(empresa.Id, usuario.Id, profesional.Id, servicio, DominioPrueba.Ahora.AddDays(-1));
        Reserva completada = Nueva(empresa.Id, usuario.Id, profesional.Id, servicio, DominioPrueba.Ahora.AddDays(-2));
        await EscenarioEmpresa.GuardarAsync(baseDatos, [servicio, profesional, .. futuras, pasada, completada]);
        await MarcarCompletadaAsync(completada.Id);

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresa.Id);
        await ambito.ServiceProvider.GetRequiredService<DarDeBajaEmpresa>().EjecutarAsync(CancellationToken.None);
        IReadOnlyList<IEventoDominio> publicados = ambito.ServiceProvider.GetRequiredService<PublicadorCaptura>().Publicados;

        IEventoDominio[] esperados =
        [
            new EmpresaDadaDeBaja(empresa.Id, DominioPrueba.Ahora),
            .. futuras.Select(r => new ReservaCancelada(r.Id, OrigenCancelacion.BajaEmpresa, DominioPrueba.Ahora)),
        ];
        publicados.Should().BeEquivalentTo(esperados, o => o.RespectingRuntimeTypes());
        EscenarioEmpresa.EntidadSeguida<Empresa>(ambito.ServiceProvider, empresa.Id).Eventos.Should().BeEmpty();
        futuras.Should().AllSatisfy(r =>
            EscenarioEmpresa.EntidadSeguida<Reserva>(ambito.ServiceProvider, r.Id).Eventos.Should().BeEmpty());
    }

    [Fact]
    public async Task Con_300_reservas_futuras_publica_301_eventos_sin_enviar_ningun_correo_dentro_de_la_operacion()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        Reserva[] futuras =
        [
            .. Enumerable.Range(1, 300).Select(h => Nueva(empresa.Id, usuario.Id, profesional.Id, servicio, DominioPrueba.Ahora.AddHours(h))),
        ];
        await EscenarioEmpresa.GuardarAsync(baseDatos, [servicio, profesional, .. futuras]);

        int correosAntes = baseDatos.Correo.Enviados.Count;
        bool fallabaAntes = baseDatos.Correo.Fallar;
        baseDatos.Correo.Fallar = true;
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresa.Id);
        Resultado<int> resultado;
        try
        {
            resultado = await ambito.ServiceProvider.GetRequiredService<DarDeBajaEmpresa>().EjecutarAsync(CancellationToken.None);
        }
        finally
        {
            baseDatos.Correo.Fallar = fallabaAntes;
        }

        resultado.EsExito.Should().BeTrue("un correo que falla no puede afectar a la baja, porque dentro de la operacion no se envia ninguno");
        baseDatos.Correo.Enviados.Count.Should().Be(correosAntes);
        resultado.Valor.Should().Be(300);
        ambito.ServiceProvider.GetRequiredService<PublicadorCaptura>().Publicados.Should().HaveCount(301)
            .And.ContainSingle(e => e is EmpresaDadaDeBaja);
    }

    [Fact]
    public async Task Con_un_publicador_que_falla_la_baja_y_las_cancelaciones_quedan_confirmadas()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        Reserva futura = Nueva(empresa.Id, usuario.Id, profesional.Id, servicio, DominioPrueba.Ahora.AddDays(1));
        await EscenarioEmpresa.GuardarAsync(baseDatos, servicio, profesional, futura);

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresa.Id);
        ambito.ServiceProvider.GetRequiredService<PublicadorCaptura>().Fallar = true;
        Func<Task> darDeBaja = () => ambito.ServiceProvider.GetRequiredService<DarDeBajaEmpresa>().EjecutarAsync(CancellationToken.None);

        await darDeBaja.Should().ThrowAsync<InvalidOperationException>();
        (await EstadoAsync(futura.Id)).Should().Be(EstadoReserva.Cancelada);
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Empresas.SingleAsync(e => e.Id == empresa.Id))).Activa.Should().BeFalse();
    }
}
