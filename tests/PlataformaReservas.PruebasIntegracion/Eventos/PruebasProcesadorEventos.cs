using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Notificaciones;
using PlataformaReservas.Aplicacion.Notificaciones;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.Eventos;
using PlataformaReservas.Infraestructura.Correo;
using PlataformaReservas.Infraestructura.Eventos;

namespace PlataformaReservas.PruebasIntegracion.Eventos;

public sealed class PruebasProcesadorEventos
{
    private static readonly DateTime Ahora = new(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Invoca_los_manejadores_del_tipo_del_evento_y_no_los_de_otros_tipos()
    {
        await using Montaje montaje = Montaje.Crear(s =>
        {
            s.AddScoped<IManejadorEvento<ReservaConfirmada>, ManejadorCaptura<ReservaConfirmada>>();
            s.AddScoped<IManejadorEvento<ReservaCancelada>, ManejadorCaptura<ReservaCancelada>>();
        });
        ReservaConfirmada evento = new(Guid.CreateVersion7(), Ahora);

        await montaje.Procesador.ProcesarAsync(evento, CancellationToken.None);

        montaje.Manejados.Should().ContainSingle().Which.Should().Be(evento);
    }

    [Fact]
    public async Task Cada_evento_se_procesa_en_un_ambito_nuevo()
    {
        await using Montaje montaje = Montaje.Crear(s =>
        {
            s.AddScoped<Marcador>();
            s.AddScoped<IManejadorEvento<ReservaConfirmada>, ManejadorDeAmbito>();
        });

        await montaje.Procesador.ProcesarAsync(new ReservaConfirmada(Guid.CreateVersion7(), Ahora), CancellationToken.None);
        await montaje.Procesador.ProcesarAsync(new ReservaConfirmada(Guid.CreateVersion7(), Ahora), CancellationToken.None);

        montaje.Ambitos.Should().HaveCount(2).And.OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Un_manejador_que_lanza_no_propaga_se_registra_como_error_y_el_siguiente_evento_se_procesa()
    {
        await using Montaje montaje = Montaje.Crear(s =>
        {
            s.AddScoped<IManejadorEvento<ReservaCancelada>, ManejadorQueFalla>();
            s.AddScoped<IManejadorEvento<ReservaConfirmada>, ManejadorCaptura<ReservaConfirmada>>();
        });
        ReservaConfirmada siguiente = new(Guid.CreateVersion7(), Ahora);

        Func<Task> fallido = () => montaje.Procesador.ProcesarAsync(
            new ReservaCancelada(Guid.CreateVersion7(), OrigenCancelacion.Cliente, Ahora), CancellationToken.None);
        await fallido.Should().NotThrowAsync();
        await montaje.Procesador.ProcesarAsync(siguiente, CancellationToken.None);

        montaje.Registro.Errores.Should().ContainSingle().Which.Should().Contain(nameof(ReservaCancelada));
        montaje.Manejados.Should().ContainSingle().Which.Should().Be(siguiente);
    }

    [Fact]
    public async Task Un_manejador_que_lanza_no_impide_que_se_ejecute_el_siguiente_manejador_del_mismo_evento()
    {
        await using Montaje montaje = Montaje.Crear(s =>
        {
            s.AddScoped<IManejadorEvento<ReservaCancelada>, ManejadorQueFalla>();
            s.AddScoped<IManejadorEvento<ReservaCancelada>, ManejadorCaptura<ReservaCancelada>>();
        });
        ReservaCancelada evento = new(Guid.CreateVersion7(), OrigenCancelacion.Cliente, Ahora);

        await montaje.Procesador.ProcesarAsync(evento, CancellationToken.None);

        montaje.Manejados.Should().ContainSingle().Which.Should().Be(evento);
        montaje.Registro.Errores.Should().ContainSingle().Which.Should().Contain(nameof(ManejadorQueFalla));
    }

    [Fact]
    public async Task Con_el_correo_caido_el_manejador_real_registra_el_fallo_y_el_procesador_sigue_con_el_siguiente_evento()
    {
        await using Montaje montaje = Montaje.Crear(s =>
        {
            s.AddScoped<IConsultasNotificacion, ConsultasConDatos>();
            s.AddScoped<IRenderizadorPlantillas, RenderizadorConAsunto>();
            s.AddScoped<IServicioEmail, CorreoQueFalla>();
            s.AddScoped<IManejadorEvento<ReservaConfirmada>, EnviarCorreosReservaConfirmada>();
            s.AddScoped<IManejadorEvento<EmpresaRegistrada>, ManejadorCaptura<EmpresaRegistrada>>();
        });
        EmpresaRegistrada siguiente = new(Guid.CreateVersion7(), Ahora);

        Func<Task> conCorreoCaido = () => montaje.Procesador.ProcesarAsync(
            new ReservaConfirmada(Guid.CreateVersion7(), Ahora), CancellationToken.None);
        await conCorreoCaido.Should().NotThrowAsync();
        await montaje.Procesador.ProcesarAsync(siguiente, CancellationToken.None);

        montaje.Registro.Errores.Should().ContainSingle().Which.Should().Contain(nameof(EnviarCorreosReservaConfirmada));
        montaje.Manejados.Should().ContainSingle().Which.Should().Be(siguiente);
    }

    [Fact]
    public async Task La_cancelacion_por_la_parada_del_host_se_propaga_sin_registrar_error()
    {
        await using Montaje montaje = Montaje.Crear(s =>
            s.AddScoped<IManejadorEvento<ReservaConfirmada>, ManejadorQueRespetaLaCancelacion>());
        using CancellationTokenSource parada = new();
        await parada.CancelAsync();

        Func<Task> procesar = () => montaje.Procesador.ProcesarAsync(
            new ReservaConfirmada(Guid.CreateVersion7(), Ahora), parada.Token);

        await procesar.Should().ThrowAsync<OperationCanceledException>();
        montaje.Registro.Errores.Should().BeEmpty();
    }

    [Fact]
    public async Task Un_evento_sin_manejadores_no_falla_ni_registra_errores()
    {
        await using Montaje montaje = Montaje.Crear(_ => { });

        Func<Task> procesar = () => montaje.Procesador.ProcesarAsync(
            new EmpresaRegistrada(Guid.CreateVersion7(), Ahora), CancellationToken.None);

        await procesar.Should().NotThrowAsync();
        montaje.Registro.Errores.Should().BeEmpty();
    }

    [Fact]
    public async Task Arrancado_procesa_lo_que_se_publica_en_la_cola_y_se_detiene_limpio()
    {
        await using Montaje montaje = Montaje.Crear(s =>
            s.AddScoped<IManejadorEvento<ReservaConfirmada>, ManejadorCaptura<ReservaConfirmada>>());
        ReservaConfirmada evento = new(Guid.CreateVersion7(), Ahora);

        await montaje.Procesador.StartAsync(CancellationToken.None);
        await montaje.Cola.PublicarAsync([evento], CancellationToken.None);
        object recibido = await montaje.PrimerManejado.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Func<Task> detener = () => montaje.Procesador.StopAsync(CancellationToken.None);

        recibido.Should().Be(evento);
        await detener.Should().CompleteWithinAsync(TimeSpan.FromSeconds(5));
    }

    private sealed class Montaje : IAsyncDisposable
    {
        private readonly ServiceProvider _servicios;

        private Montaje(ServiceProvider servicios)
        {
            _servicios = servicios;
            Cola = servicios.GetRequiredService<ColaEventos>();
            Registro = servicios.GetRequiredService<RegistroCaptura>();
            Procesador = new ProcesadorEventos(Cola, servicios.GetRequiredService<IServiceScopeFactory>(), Registro);
        }

        public ColaEventos Cola { get; }

        public RegistroCaptura Registro { get; }

        public ProcesadorEventos Procesador { get; }

        public ConcurrentQueue<object> Manejados => _servicios.GetRequiredService<Capturas>().Manejados;

        public ConcurrentQueue<Guid> Ambitos => _servicios.GetRequiredService<Capturas>().Ambitos;

        public TaskCompletionSource<object> PrimerManejado => _servicios.GetRequiredService<Capturas>().PrimerManejado;

        public static Montaje Crear(Action<IServiceCollection> manejadores)
        {
            ServiceCollection servicios = new();
            servicios.AddSingleton<ColaEventos>();
            servicios.AddSingleton<RegistroCaptura>();
            servicios.AddSingleton<Capturas>();
            manejadores(servicios);
            return new Montaje(servicios.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true }));
        }

        public async ValueTask DisposeAsync()
        {
            Procesador.Dispose();
            await _servicios.DisposeAsync();
        }
    }

    private sealed class Capturas
    {
        public ConcurrentQueue<object> Manejados { get; } = new();

        public ConcurrentQueue<Guid> Ambitos { get; } = new();

        public TaskCompletionSource<object> PrimerManejado { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class Marcador
    {
        public Guid Id { get; } = Guid.CreateVersion7();
    }

    private sealed class ManejadorCaptura<TEvento>(Capturas capturas) : IManejadorEvento<TEvento>
        where TEvento : IEventoDominio
    {
        public Task ManejarAsync(TEvento evento, CancellationToken ct)
        {
            capturas.Manejados.Enqueue(evento);
            capturas.PrimerManejado.TrySetResult(evento);
            return Task.CompletedTask;
        }
    }

    private sealed class ManejadorDeAmbito(Capturas capturas, Marcador marcador) : IManejadorEvento<ReservaConfirmada>
    {
        public Task ManejarAsync(ReservaConfirmada evento, CancellationToken ct)
        {
            capturas.Ambitos.Enqueue(marcador.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class ManejadorQueFalla : IManejadorEvento<ReservaCancelada>
    {
        public Task ManejarAsync(ReservaCancelada evento, CancellationToken ct)
        {
            throw new InvalidOperationException("El manejador falla a proposito.");
        }
    }

    private sealed class ConsultasConDatos : IConsultasNotificacion
    {
        public Task<DatosReservaNotificacionDto?> ObtenerDatosReservaAsync(Guid reservaId, CancellationToken ct)
        {
            return Task.FromResult<DatosReservaNotificacionDto?>(new DatosReservaNotificacionDto(
                reservaId, "Barberia Centro", "Calle Mayor 1, 37001 Salamanca", "contacto@barberia.es", "Corte", "Ana Garcia", Ahora, 45,
                18.50m, "Lucia Fernandez", "611222333", "lucia@ejemplo.es"));
        }

        public Task<DatosEmpresaNotificacionDto?> ObtenerDatosEmpresaAsync(Guid empresaId, DateTime instanteEvento, CancellationToken ct)
        {
            return Task.FromResult<DatosEmpresaNotificacionDto?>(null);
        }

        public Task<DatosCuentaNotificacionDto?> ObtenerDatosCuentaAsync(Guid usuarioId, DateTime instanteEvento, CancellationToken ct)
        {
            return Task.FromResult<DatosCuentaNotificacionDto?>(null);
        }
    }

    private sealed class RenderizadorConAsunto : IRenderizadorPlantillas
    {
        public Task<MensajeCorreo> ReservaConfirmadaClienteAsync(DatosReservaNotificacionDto datos) => Mensaje(datos.EmailCliente);

        public Task<MensajeCorreo> ReservaConfirmadaNegocioAsync(DatosReservaNotificacionDto datos) => Mensaje(datos.EmailEstablecimiento);

        public Task<MensajeCorreo> ReservaCanceladaPorClienteAsync(DatosReservaNotificacionDto datos) => throw new NotSupportedException();

        public Task<MensajeCorreo> ReservaCanceladaPorEmpresaAsync(DatosReservaNotificacionDto datos) => throw new NotSupportedException();

        public Task<MensajeCorreo> ReservaCanceladaPorBajaEmpresaAsync(DatosReservaNotificacionDto datos) => throw new NotSupportedException();

        public Task<MensajeCorreo> ReservaCanceladaPorBajaCuentaAsync(DatosReservaNotificacionDto datos) => throw new NotSupportedException();

        public Task<MensajeCorreo> EmpresaRegistradaAsync(DatosEmpresaNotificacionDto datos) => throw new NotSupportedException();

        public Task<MensajeCorreo> EmpresaDadaDeBajaAsync(DatosEmpresaNotificacionDto datos) => throw new NotSupportedException();

        public Task<MensajeCorreo> CuentaRegistradaAsync(DatosCuentaNotificacionDto datos) => throw new NotSupportedException();

        public Task<MensajeCorreo> CuentaDadaDeBajaAsync(DatosCuentaNotificacionDto datos) => throw new NotSupportedException();

        public Task<MensajeCorreo> RestablecimientoClaveAsync(string correo, string nombre, string enlace) => throw new NotSupportedException();

        private static Task<MensajeCorreo> Mensaje(string para) => Task.FromResult(new MensajeCorreo(para, "Asunto", "<p>Cuerpo</p>"));
    }

    private sealed class CorreoQueFalla : IServicioEmail
    {
        public Task EnviarAsync(MensajeCorreo mensaje, CancellationToken ct)
        {
            throw new CorreoNoEnviadoException("SocketException");
        }
    }

    private sealed class ManejadorQueRespetaLaCancelacion : IManejadorEvento<ReservaConfirmada>
    {
        public async Task ManejarAsync(ReservaConfirmada evento, CancellationToken ct)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        }
    }

    private sealed class RegistroCaptura : ILogger<ProcesadorEventos>
    {
        public ConcurrentQueue<string> Errores { get; } = new();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error)
            {
                Errores.Enqueue(formatter(state, exception));
            }
        }
    }
}
