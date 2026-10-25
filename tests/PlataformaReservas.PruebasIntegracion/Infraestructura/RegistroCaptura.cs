using Microsoft.Extensions.Logging;

namespace PlataformaReservas.PruebasIntegracion.Infraestructura;

public sealed class RegistroCaptura<T> : ILogger<T>
{
    public List<(LogLevel Nivel, string Linea)> Entradas { get; } = [];

    public IReadOnlyList<string> LineasDe(LogLevel nivel) => Entradas.Where(e => e.Nivel == nivel).Select(e => e.Linea).ToList();

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Entradas.Add((logLevel, formatter(state, exception)));
    }
}
