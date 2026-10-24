using FluentAssertions;
using Microsoft.Extensions.Logging;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Infraestructura.Correo;

namespace PlataformaReservas.PruebasIntegracion.Correo;

public sealed class PruebasServicioEmailRegistro
{
    [Fact]
    public async Task Registra_el_destinatario_enmascarado_y_el_asunto_pero_nunca_el_cuerpo()
    {
        RegistroCaptura registro = new();
        MensajeCorreo mensaje = new(
            "lucia.perez@ejemplo.es", "Restablece tu contraseña",
            "<p>Pulsa <a href=\"https://localhost/Account/ResetPassword?code=TOKEN-SECRETO\">aquí</a></p>");

        await new ServicioEmailRegistro(registro).EnviarAsync(mensaje, CancellationToken.None);

        string linea = registro.Lineas.Should().ContainSingle().Subject;
        registro.Niveles.Should().Equal(LogLevel.Information);
        linea.Should().Contain("l***@ejemplo.es").And.Contain("Restablece tu contraseña");
        linea.Should().NotContain("lucia.perez").And.NotContain("TOKEN-SECRETO").And.NotContain("<p>");
    }

    [Theory]
    [InlineData("ana@ejemplo.es", "a***@ejemplo.es")]
    [InlineData("a@b.es", "a***@b.es")]
    [InlineData("sin-arroba", "***")]
    [InlineData("@ejemplo.es", "***")]
    public void Enmascara_todo_salvo_la_primera_letra_y_el_dominio(string correo, string esperado)
    {
        ServicioEmailRegistro.Enmascarar(correo).Should().Be(esperado);
    }

    [Fact]
    public async Task No_envia_nada_y_termina_sin_error()
    {
        Func<Task> enviar = () => new ServicioEmailRegistro(new RegistroCaptura())
            .EnviarAsync(new MensajeCorreo("ana@ejemplo.es", "Asunto", "<p>Cuerpo</p>"), CancellationToken.None);

        await enviar.Should().NotThrowAsync();
    }

    private sealed class RegistroCaptura : ILogger<ServicioEmailRegistro>
    {
        public List<string> Lineas { get; } = [];

        public List<LogLevel> Niveles { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Niveles.Add(logLevel);
            Lineas.Add(formatter(state, exception));
        }
    }
}
