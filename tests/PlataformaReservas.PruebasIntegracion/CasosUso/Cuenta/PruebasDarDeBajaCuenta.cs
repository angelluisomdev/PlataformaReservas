using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.CasosUso.Cuenta;
using PlataformaReservas.Aplicacion.CasosUso.Reservas;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Disponibilidad;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.Eventos;
using PlataformaReservas.Dominio.Repositorios;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Cuenta;

[Collection("BaseDatos")]
public sealed class PruebasDarDeBajaCuenta(BaseDatosFixture baseDatos)
{
    private static readonly DateTime Ahora = DominioPrueba.Ahora;

    [Fact]
    public async Task Cancela_solo_las_futuras_confirmadas_y_deja_intactas_las_demas()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva futura1 = escenario.Reserva(Ahora.AddDays(1));
        Reserva futura2 = escenario.Reserva(Ahora.AddDays(2));
        Reserva pasada = escenario.Reserva(Ahora.AddDays(-1));
        Reserva completada = escenario.Reserva(Ahora.AddDays(-2));
        Reserva yaCancelada = escenario.Reserva(Ahora.AddDays(3));
        yaCancelada.CancelarPorCliente(Ahora.AddHours(-1), OrigenCancelacion.Cliente);
        await EscenarioEmpresa.GuardarAsync(baseDatos, futura1, futura2, pasada, completada, yaCancelada);
        await EjecutarSqlAsync($"UPDATE reservas SET estado = {(int)EstadoReserva.Completada} WHERE id = {completada.Id}");

        Resultado<int> resultado = await DarDeBajaAsync(escenario.Cliente.Id);

        resultado.EsExito.Should().BeTrue();
        resultado.Valor.Should().Be(2);
        (await LeerAsync(futura1.Id)).Estado.Should().Be(EstadoReserva.Cancelada);
        (await LeerAsync(futura2.Id)).FechaCancelacion.Should().Be(Ahora);
        (await LeerAsync(pasada.Id)).Estado.Should().Be(EstadoReserva.Confirmada);
        (await LeerAsync(completada.Id)).Estado.Should().Be(EstadoReserva.Completada);
        (await LeerAsync(yaCancelada.Id)).FechaCancelacion.Should().Be(Ahora.AddHours(-1));
    }

    [Fact]
    public async Task Desactiva_la_cuenta_renueva_el_sello_y_ya_no_puede_iniciar_sesion()
    {
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        string? selloAntes = await SelloAsync(usuario.Id);

        (await DarDeBajaAsync(usuario.Id)).EsExito.Should().BeTrue();

        (await UsuarioAsync(usuario.Id)).Activo.Should().BeFalse();
        (await SelloAsync(usuario.Id)).Should().NotBe(selloAntes);
        (await UsuariosPrueba.ComprobarClaveAsync(baseDatos, usuario.Id, UsuariosPrueba.Clave)).Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Un_propietario_con_empresa_activa_da_conflicto_y_no_cambia_nada()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        await EscenarioEmpresa.GuardarAsync(baseDatos, MiembroEmpresa.Crear(
            Guid.CreateVersion7(), escenario.Empresa.Id, escenario.Cliente.Id, RolMiembro.Propietario, Ahora));
        Reserva futura = escenario.Reserva(Ahora.AddDays(1));
        await EscenarioEmpresa.GuardarAsync(baseDatos, futura);

        Resultado<int> resultado = await DarDeBajaAsync(escenario.Cliente.Id);

        resultado.Codigo.Should().Be(CodigoError.Conflicto);
        (await UsuarioAsync(escenario.Cliente.Id)).Activo.Should().BeTrue();
        (await LeerAsync(futura.Id)).Estado.Should().Be(EstadoReserva.Confirmada);
    }

    [Fact]
    public async Task Un_propietario_con_la_empresa_ya_dada_de_baja_si_puede_darse_de_baja()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        await EscenarioEmpresa.GuardarAsync(baseDatos, MiembroEmpresa.Crear(
            Guid.CreateVersion7(), empresa.Id, usuario.Id, RolMiembro.Propietario, Ahora));
        await EjecutarSqlAsync($"UPDATE empresas SET activa = false WHERE id = {empresa.Id}");

        (await DarDeBajaAsync(usuario.Id)).EsExito.Should().BeTrue();

        (await UsuarioAsync(usuario.Id)).Activo.Should().BeFalse();
    }

    [Fact]
    public async Task Tras_confirmar_publica_la_baja_y_una_cancelacion_por_cada_reserva()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva futura1 = escenario.Reserva(Ahora.AddDays(1));
        Reserva futura2 = escenario.Reserva(Ahora.AddDays(2));
        await EscenarioEmpresa.GuardarAsync(baseDatos, futura1, futura2);

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(escenario.Cliente.Id);
        await ambito.ServiceProvider.GetRequiredService<DarDeBajaCuenta>().EjecutarAsync(CancellationToken.None);

        IEventoDominio[] esperados =
        [
            new CuentaDadaDeBaja(escenario.Cliente.Id, Ahora),
            new ReservaCancelada(futura1.Id, OrigenCancelacion.BajaCuenta, Ahora),
            new ReservaCancelada(futura2.Id, OrigenCancelacion.BajaCuenta, Ahora),
        ];
        ambito.ServiceProvider.GetRequiredService<PublicadorCaptura>().Publicados
            .Should().BeEquivalentTo(esperados, o => o.RespectingRuntimeTypes());
        EscenarioEmpresa.EntidadSeguida<Reserva>(ambito.ServiceProvider, futura1.Id).Eventos.Should().BeEmpty();
        EscenarioEmpresa.EntidadSeguida<Reserva>(ambito.ServiceProvider, futura2.Id).Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Un_fallo_a_mitad_no_deja_nada_aplicado()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva futura = escenario.Reserva(Ahora.AddDays(1));
        await EscenarioEmpresa.GuardarAsync(baseDatos, futura);

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(escenario.Cliente.Id);
        IServiceProvider servicios = ambito.ServiceProvider;
        DarDeBajaCuenta darDeBaja = new(
            servicios.GetRequiredService<IContextoUsuario>(),
            new CuentasQueFallanAlDarDeBaja(servicios.GetRequiredService<IServicioCuentas>()),
            servicios.GetRequiredService<IRepositorioMiembros>(),
            servicios.GetRequiredService<IConsultasDisponibilidad>(),
            servicios.GetRequiredService<IRepositorioReservasCliente>(),
            servicios.GetRequiredService<IUnidadTrabajo>(),
            servicios.GetRequiredService<IPublicadorEventos>(),
            servicios.GetRequiredService<IRelojSistema>());

        Func<Task> ejecutar = () => darDeBaja.EjecutarAsync(CancellationToken.None);

        await ejecutar.Should().ThrowAsync<InvalidOperationException>();
        (await LeerAsync(futura.Id)).Estado.Should().Be(EstadoReserva.Confirmada);
        (await UsuarioAsync(escenario.Cliente.Id)).Activo.Should().BeTrue();
        servicios.GetRequiredService<PublicadorCaptura>().Publicados.Should().BeEmpty();
    }

    [Fact]
    public async Task Sin_sesion_da_no_autorizado()
    {
        (await DarDeBajaAsync(null)).Codigo.Should().Be(CodigoError.NoAutorizado);
    }

    [Fact]
    public async Task Darse_de_baja_dos_veces_da_estado_no_valido()
    {
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        await DarDeBajaAsync(usuario.Id);

        (await DarDeBajaAsync(usuario.Id)).Codigo.Should().Be(CodigoError.EstadoNoValido);
    }

    [Fact]
    public async Task Una_cuenta_ya_inactiva_da_estado_no_valido_antes_de_tocar_ninguna_reserva()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva futura = escenario.Reserva(Ahora.AddDays(1));
        await EscenarioEmpresa.GuardarAsync(baseDatos, futura);
        await UsuariosPrueba.DesactivarAsync(baseDatos, escenario.Cliente.Id);
        string? selloAntes = await SelloAsync(escenario.Cliente.Id);

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(escenario.Cliente.Id);
        Resultado<int> resultado = await ambito.ServiceProvider.GetRequiredService<DarDeBajaCuenta>().EjecutarAsync(CancellationToken.None);

        resultado.Codigo.Should().Be(CodigoError.EstadoNoValido);
        ambito.ServiceProvider.GetRequiredService<ContextoDatos>().ChangeTracker.Entries<Reserva>().Should().BeEmpty();
        (await LeerAsync(futura.Id)).Estado.Should().Be(EstadoReserva.Confirmada);
        (await SelloAsync(escenario.Cliente.Id)).Should().Be(selloAntes);
        ambito.ServiceProvider.GetRequiredService<PublicadorCaptura>().Publicados.Should().BeEmpty();
    }

    [Fact]
    public async Task Si_el_usuario_de_la_sesion_no_existe_lanza_porque_es_un_estado_imposible()
    {
        Func<Task> ejecutar = () => DarDeBajaAsync(Guid.CreateVersion7());

        await ejecutar.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task La_reserva_de_otro_cliente_no_se_toca_y_el_hueco_propio_vuelve_a_ofrecerse()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Usuario otro = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        Reserva propia = escenario.Reserva(EscenarioReserva.NueveLunesUtc);
        Reserva deOtro = escenario.Reserva(EscenarioReserva.NueveLunesUtc.AddHours(2), otro.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, propia, deOtro);

        await DarDeBajaAsync(escenario.Cliente.Id);

        (await LeerAsync(deOtro.Id)).Estado.Should().Be(EstadoReserva.Confirmada);
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(null);
        Resultado<IReadOnlyList<HuecoDisponible>> huecos = await ambito.ServiceProvider.GetRequiredService<ConsultarDisponibilidad>()
            .EjecutarAsync(new ConsultarDisponibilidadComando(
                escenario.Empresa.Id, escenario.Servicio.Id, escenario.Profesional.Id, EscenarioReserva.Lunes), CancellationToken.None);
        huecos.Valor.Should().Contain(h => h.InicioUtc == EscenarioReserva.NueveLunesUtc);
    }

    private async Task<Resultado<int>> DarDeBajaAsync(Guid? usuarioId)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(usuarioId);
        return await ambito.ServiceProvider.GetRequiredService<DarDeBajaCuenta>().EjecutarAsync(CancellationToken.None);
    }

    private Task<Reserva> LeerAsync(Guid id)
    {
        return EscenarioEmpresa.LeerAsync(baseDatos, db => db.Reservas.SingleAsync(r => r.Id == id));
    }

    private async Task<Usuario> UsuarioAsync(Guid id)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        return (await ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>().FindByIdAsync(id.ToString()))!;
    }

    private async Task<string?> SelloAsync(Guid id)
    {
        return (await UsuarioAsync(id)).SecurityStamp;
    }

    private async Task EjecutarSqlAsync(FormattableString sql)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        await ambito.ServiceProvider.GetRequiredService<ContextoDatos>().Database.ExecuteSqlInterpolatedAsync(sql);
    }
}
