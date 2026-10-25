using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.CasosUso.Reservas;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.Eventos;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Reservas;

[Collection("BaseDatos")]
public sealed class PruebasCrearReserva(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Crea_la_reserva_confirmada_con_las_copias_y_el_fin_calculado_en_servidor()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);

        Resultado<Guid> resultado = await CrearAsync(escenario.Cliente.Id, escenario.Comando(observaciones: "  Llegare puntual  "));

        resultado.EsExito.Should().BeTrue();
        Reserva reserva = await LeerReservaAsync(resultado.Valor);
        reserva.Estado.Should().Be(EstadoReserva.Confirmada);
        reserva.EmpresaId.Should().Be(escenario.Empresa.Id);
        reserva.UsuarioId.Should().Be(escenario.Cliente.Id);
        reserva.ProfesionalId.Should().Be(escenario.Profesional.Id);
        reserva.ServicioId.Should().Be(escenario.Servicio.Id);
        reserva.Franja.InicioUtc.Should().Be(EscenarioReserva.NueveLunesUtc);
        reserva.Franja.FinUtc.Should().Be(EscenarioReserva.NueveLunesUtc.AddMinutes(45));
        reserva.PrecioAplicado.Should().Be(new Precio(18.50m));
        reserva.DuracionAplicada.Should().Be(new Duracion(45));
        reserva.NombreCliente.Should().Be(new NombrePersona("Lucia Fernandez"));
        reserva.TelefonoCliente.Should().Be(new Telefono(EscenarioReserva.TelefonoCliente));
        reserva.Observaciones.Should().Be(new Observaciones("Llegare puntual"));
        reserva.FechaAlta.Should().Be(DominioPrueba.Ahora);
    }

    [Fact]
    public async Task Sin_usuario_da_no_autorizado()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);

        Resultado<Guid> resultado = await CrearAsync(null, escenario.Comando());

        resultado.Codigo.Should().Be(CodigoError.NoAutorizado);
        (await ReservasDelProfesionalAsync(escenario.Profesional.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Una_empresa_inactiva_da_validacion()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        await ModificarAsync(db => db.Empresas.AsTracking().SingleAsync(e => e.Id == escenario.Empresa.Id), e => e.DarDeBaja(DominioPrueba.Ahora));

        await DebeDarValidacionSinCrearNadaAsync(escenario, escenario.Comando());
    }

    [Fact]
    public async Task Una_empresa_inexistente_da_validacion()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);

        await DebeDarValidacionSinCrearNadaAsync(escenario, escenario.Comando() with { EmpresaId = Guid.CreateVersion7() });
    }

    [Fact]
    public async Task Un_servicio_inexistente_da_validacion()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);

        await DebeDarValidacionSinCrearNadaAsync(escenario, escenario.Comando() with { ServicioId = Guid.CreateVersion7() });
    }

    [Fact]
    public async Task Un_servicio_inactivo_da_validacion()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        await ModificarAsync(db => db.Servicios.AsTracking().SingleAsync(s => s.Id == escenario.Servicio.Id), s => s.Desactivar(DominioPrueba.Ahora));

        await DebeDarValidacionSinCrearNadaAsync(escenario, escenario.Comando());
    }

    [Fact]
    public async Task Un_servicio_de_otra_empresa_da_validacion()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        EscenarioReserva ajeno = await EscenarioReserva.CrearAsync(baseDatos);

        await DebeDarValidacionSinCrearNadaAsync(escenario, escenario.Comando() with { ServicioId = ajeno.Servicio.Id });
    }

    [Fact]
    public async Task Un_profesional_inexistente_da_validacion()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);

        await DebeDarValidacionSinCrearNadaAsync(escenario, escenario.Comando() with { ProfesionalId = Guid.CreateVersion7() });
    }

    [Fact]
    public async Task Un_profesional_inactivo_da_validacion()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        await ModificarAsync(db => db.Profesionales.AsTracking().SingleAsync(p => p.Id == escenario.Profesional.Id), p => p.Desactivar());

        await DebeDarValidacionSinCrearNadaAsync(escenario, escenario.Comando());
    }

    [Fact]
    public async Task Un_profesional_de_otra_empresa_da_validacion()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        EscenarioReserva ajeno = await EscenarioReserva.CrearAsync(baseDatos);

        await DebeDarValidacionSinCrearNadaAsync(escenario, escenario.Comando() with { ProfesionalId = ajeno.Profesional.Id });
        (await ReservasDelProfesionalAsync(ajeno.Profesional.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Un_profesional_que_no_presta_el_servicio_da_validacion()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Servicio otroServicio = DominioPrueba.Servicio(escenario.Empresa.Id);
        Profesional noLoPresta = EscenarioReserva.ProfesionalQuePresta(escenario.Empresa.Id, otroServicio.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, otroServicio, noLoPresta);

        await DebeDarValidacionSinCrearNadaAsync(escenario, escenario.Comando() with { ProfesionalId = noLoPresta.Id });
        (await ReservasDelProfesionalAsync(noLoPresta.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Un_usuario_sin_telefono_da_validacion()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos, telefono: null);

        await DebeDarValidacionSinCrearNadaAsync(escenario, escenario.Comando());
    }

    [Fact]
    public async Task Unas_observaciones_demasiado_largas_dan_validacion()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);

        await DebeDarValidacionSinCrearNadaAsync(
            escenario, escenario.Comando(observaciones: new string('a', Observaciones.LongitudMaxima + 1)));
    }

    [Fact]
    public async Task Un_hueco_dentro_de_la_antelacion_minima_da_hueco_ya_no_disponible()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        DateTime dentroDeLaAntelacion = new(2026, 10, 3, 8, 30, 0, DateTimeKind.Utc);

        Resultado<Guid> resultado = await CrearAsync(
            escenario.Cliente.Id, escenario.Comando(EscenarioReserva.Sabado, dentroDeLaAntelacion));

        resultado.Codigo.Should().Be(CodigoError.HuecoYaNoDisponible);
        (await ReservasDelProfesionalAsync(escenario.Profesional.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Un_hueco_justo_en_el_limite_de_la_antelacion_minima_se_acepta()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        DateTime enElLimite = DominioPrueba.Ahora.AddMinutes(60);

        Resultado<Guid> resultado = await CrearAsync(escenario.Cliente.Id, escenario.Comando(EscenarioReserva.Sabado, enElLimite));

        resultado.EsExito.Should().BeTrue();
    }

    [Fact]
    public async Task Un_hueco_fuera_de_la_ventana_da_hueco_ya_no_disponible()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        DateOnly fueraDeLaVentana = EscenarioReserva.Lunes.AddDays(63);
        DateTime nueveLocalEnInvierno = new(2026, 12, 7, 8, 0, 0, DateTimeKind.Utc);

        Resultado<Guid> resultado = await CrearAsync(
            escenario.Cliente.Id, escenario.Comando(fueraDeLaVentana, nueveLocalEnInvierno));

        resultado.Codigo.Should().Be(CodigoError.HuecoYaNoDisponible);
        (await ReservasDelProfesionalAsync(escenario.Profesional.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Una_fecha_que_no_corresponde_al_inicio_da_hueco_ya_no_disponible()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);

        Resultado<Guid> resultado = await CrearAsync(
            escenario.Cliente.Id, escenario.Comando(EscenarioReserva.Lunes.AddDays(7)));

        resultado.Codigo.Should().Be(CodigoError.HuecoYaNoDisponible);
        (await ReservasDelProfesionalAsync(escenario.Profesional.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Un_hueco_ocupado_por_una_reserva_confirmada_da_hueco_ya_no_disponible_y_no_deja_nada()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        await EscenarioEmpresa.GuardarAsync(baseDatos, escenario.Reserva(EscenarioReserva.NueveLunesUtc.AddMinutes(-30)));

        Resultado<Guid> resultado = await CrearAsync(escenario.Cliente.Id, escenario.Comando());

        resultado.Codigo.Should().Be(CodigoError.HuecoYaNoDisponible);
        (await ReservasDelProfesionalAsync(escenario.Profesional.Id)).Should().Be(1);
    }

    [Fact]
    public async Task Una_reserva_contigua_a_otra_se_acepta()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        await EscenarioEmpresa.GuardarAsync(baseDatos, escenario.Reserva(EscenarioReserva.NueveLunesUtc));

        Resultado<Guid> resultado = await CrearAsync(
            escenario.Cliente.Id, escenario.Comando(inicioUtc: EscenarioReserva.NueveLunesUtc.AddMinutes(45)));

        resultado.EsExito.Should().BeTrue();
        (await ReservasDelProfesionalAsync(escenario.Profesional.Id)).Should().Be(2);
    }

    [Fact]
    public async Task El_precio_copiado_se_conserva_aunque_el_servicio_cambie_despues()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Resultado<Guid> resultado = await CrearAsync(escenario.Cliente.Id, escenario.Comando());

        await ModificarAsync(
            db => db.Servicios.AsTracking().SingleAsync(s => s.Id == escenario.Servicio.Id),
            s => s.ActualizarDatos(s.Nombre, s.Descripcion, new Duracion(60), new Precio(30m), DominioPrueba.Ahora));

        Reserva reserva = await LeerReservaAsync(resultado.Valor);
        reserva.PrecioAplicado.Should().Be(new Precio(18.50m));
        reserva.DuracionAplicada.Should().Be(new Duracion(45));
        reserva.Franja.FinUtc.Should().Be(EscenarioReserva.NueveLunesUtc.AddMinutes(45));
    }

    private async Task DebeDarValidacionSinCrearNadaAsync(EscenarioReserva escenario, CrearReservaComando comando)
    {
        Resultado<Guid> resultado = await CrearAsync(escenario.Cliente.Id, comando);

        resultado.EsExito.Should().BeFalse();
        resultado.Codigo.Should().Be(CodigoError.Validacion);
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Reservas.CountAsync(r => r.UsuarioId == escenario.Cliente.Id)))
            .Should().Be(0);
    }

    private async Task<Resultado<Guid>> CrearAsync(Guid? usuarioId, CrearReservaComando comando)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(usuarioId);
        return await ambito.ServiceProvider.GetRequiredService<CrearReserva>().EjecutarAsync(comando, CancellationToken.None);
    }

    private Task<Reserva> LeerReservaAsync(Guid id)
    {
        return EscenarioEmpresa.LeerAsync(baseDatos, db => db.Reservas.SingleAsync(r => r.Id == id));
    }

    private Task<int> ReservasDelProfesionalAsync(Guid profesionalId)
    {
        return EscenarioEmpresa.LeerAsync(baseDatos, db => db.Reservas.CountAsync(r => r.ProfesionalId == profesionalId));
    }

    private async Task ModificarAsync<T>(Func<ContextoDatos, Task<T>> cargar, Action<T> cambio)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();
        cambio(await cargar(db));
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Tras_confirmar_publica_un_unico_evento_de_reserva_confirmada()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(escenario.Cliente.Id);
        Resultado<Guid> resultado = await ambito.ServiceProvider.GetRequiredService<CrearReserva>()
            .EjecutarAsync(escenario.Comando(), CancellationToken.None);

        ambito.ServiceProvider.GetRequiredService<PublicadorCaptura>().Publicados.Should().ContainSingle()
            .Which.Should().Be(new ReservaConfirmada(resultado.Valor, DominioPrueba.Ahora));
        EscenarioEmpresa.EntidadSeguida<Reserva>(ambito.ServiceProvider, resultado.Valor).Eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Una_validacion_o_un_hueco_ocupado_no_publican_nada()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        await EscenarioEmpresa.GuardarAsync(baseDatos, escenario.Reserva(EscenarioReserva.NueveLunesUtc));

        (Resultado<Guid> invalida, IReadOnlyList<IEventoDominio> trasValidacion) = await CrearYCapturarAsync(
            escenario.Cliente.Id, escenario.Comando(observaciones: new string('a', Observaciones.LongitudMaxima + 1)));
        (Resultado<Guid> ocupada, IReadOnlyList<IEventoDominio> trasHuecoOcupado) =
            await CrearYCapturarAsync(escenario.Cliente.Id, escenario.Comando());

        invalida.Codigo.Should().Be(CodigoError.Validacion);
        ocupada.Codigo.Should().Be(CodigoError.HuecoYaNoDisponible);
        trasValidacion.Should().BeEmpty();
        trasHuecoOcupado.Should().BeEmpty();
    }

    [Fact]
    public async Task Con_un_publicador_que_falla_la_reserva_queda_confirmada()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(escenario.Cliente.Id);
        ambito.ServiceProvider.GetRequiredService<PublicadorCaptura>().Fallar = true;
        Func<Task> crear = () => ambito.ServiceProvider.GetRequiredService<CrearReserva>()
            .EjecutarAsync(escenario.Comando(), CancellationToken.None);

        await crear.Should().ThrowAsync<InvalidOperationException>();
        Reserva reserva = await EscenarioEmpresa.LeerAsync(
            baseDatos, db => db.Reservas.SingleAsync(r => r.ProfesionalId == escenario.Profesional.Id));
        reserva.Estado.Should().Be(EstadoReserva.Confirmada);
    }

    private async Task<(Resultado<Guid> Resultado, IReadOnlyList<IEventoDominio> Publicados)> CrearYCapturarAsync(
        Guid usuarioId, CrearReservaComando comando)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeUsuario(usuarioId);
        Resultado<Guid> resultado = await ambito.ServiceProvider.GetRequiredService<CrearReserva>()
            .EjecutarAsync(comando, CancellationToken.None);
        return (resultado, ambito.ServiceProvider.GetRequiredService<PublicadorCaptura>().Publicados);
    }
}
