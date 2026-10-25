using FluentAssertions;
using Microsoft.Extensions.Logging;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Infraestructura.Correo;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Correo;

public sealed class PruebasServicioEmailRegistro
{
    [Fact]
    public async Task Registra_el_destinatario_enmascarado_y_el_asunto_pero_nunca_el_cuerpo()
    {
        RegistroCaptura<ServicioEmailRegistro> registro = new();
        MensajeCorreo mensaje = new(
            "lucia.perez@ejemplo.es", "Restablece tu contraseña",
            "<p>Pulsa <a href=\"https://localhost/Account/ResetPassword?code=TOKEN-SECRETO\">aquí</a></p>");

        await new ServicioEmailRegistro(registro).EnviarAsync(mensaje, CancellationToken.None);

        (LogLevel nivel, string linea) = registro.Entradas.Should().ContainSingle().Subject;
        nivel.Should().Be(LogLevel.Information);
        linea.Should().Contain("l***@ejemplo.es").And.Contain("Restablece tu contraseña");
        linea.Should().NotContain("lucia.perez").And.NotContain("TOKEN-SECRETO").And.NotContain("<p>");
    }

    [Fact]
    public async Task No_envia_nada_y_termina_sin_error()
    {
        Func<Task> enviar = () => new ServicioEmailRegistro(new RegistroCaptura<ServicioEmailRegistro>())
            .EnviarAsync(new MensajeCorreo("ana@ejemplo.es", "Asunto", "<p>Cuerpo</p>"), CancellationToken.None);

        await enviar.Should().NotThrowAsync();
    }
}
