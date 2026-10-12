using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Dominio.Eventos;

namespace PlataformaReservas.Infraestructura.Eventos;

public sealed partial class ProcesadorEventos(ColaEventos cola, IServiceScopeFactory ambitos, ILogger<ProcesadorEventos> registro)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (IEventoDominio evento in cola.LeerAsync(stoppingToken))
        {
            await ProcesarAsync(evento, stoppingToken);
        }
    }

    // Nunca propaga: una excepcion que escape mata el BackgroundService y no vuelve a procesarse ningun evento (R-6).
    // Solo deja pasar la cancelacion de la parada del host, que no es un fallo.
    internal async Task ProcesarAsync(IEventoDominio evento, CancellationToken ct)
    {
        try
        {
            await using AsyncServiceScope ambito = ambitos.CreateAsyncScope();
            Type tipoManejador = typeof(IManejadorEvento<>).MakeGenericType(evento.GetType());
            foreach (object? manejador in ambito.ServiceProvider.GetServices(tipoManejador))
            {
                await (Task)tipoManejador.GetMethod(nameof(IManejadorEvento<IEventoDominio>.ManejarAsync))!
                    .Invoke(manejador, [evento, ct])!;
            }
        }
        catch (Exception error) when (error is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            RegistrarFallo(registro, error, evento.GetType().Name);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Fallo al procesar el evento {TipoEvento}")]
    private static partial void RegistrarFallo(ILogger registro, Exception error, string tipoEvento);
}
