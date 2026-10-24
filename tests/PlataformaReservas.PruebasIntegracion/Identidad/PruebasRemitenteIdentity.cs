using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Correo;
using PlataformaReservas.Infraestructura.Identidad;

namespace PlataformaReservas.PruebasIntegracion.Identidad;

public sealed class PruebasRemitenteIdentity : IAsyncDisposable
{
    private readonly ServiceProvider _servicios = new ServiceCollection().AddLogging().BuildServiceProvider();
    private readonly RenderizadorPlantillas _plantillas;
    private readonly CorreoCapturado _correo = new();

    public PruebasRemitenteIdentity()
    {
        _plantillas = new RenderizadorPlantillas(_servicios, NullLoggerFactory.Instance, new ConfigurationBuilder().Build());
    }

    [Fact]
    public async Task Envia_el_restablecimiento_al_correo_indicado_con_el_enlace_que_dio_identity()
    {
        const string enlace = "https://reservas.example/Account/ResetPassword?code=AbC-123_x&otro=1";
        Usuario usuario = Usuario.Crear("lucia@ejemplo.es", new NombrePersona("Lucia Fernandez"), null, DateTime.UtcNow);

        await new RemitenteIdentity(_correo, _plantillas)
            .SendPasswordResetLinkAsync(usuario, "lucia@ejemplo.es", enlace);

        MensajeCorreo mensaje = _correo.Enviados.Should().ContainSingle().Subject;
        mensaje.Para.Should().Be("lucia@ejemplo.es");
        mensaje.Asunto.Should().Be("Restablece tu contraseña");
        mensaje.CuerpoHtml.Should()
            .Contain("Lucia Fernandez")
            .And.Contain("href=\"https://reservas.example/Account/ResetPassword?code=AbC-123_x&amp;otro=1\"")
            .And.NotContain("&amp;amp;");
    }

    [Fact]
    public async Task Los_metodos_sin_uso_en_el_mvp_no_envian_nada()
    {
        Usuario usuario = Usuario.Crear("lucia@ejemplo.es", new NombrePersona("Lucia Fernandez"), null, DateTime.UtcNow);
        RemitenteIdentity remitente = new(_correo, _plantillas);

        Func<Task> confirmacion = () => remitente.SendConfirmationLinkAsync(usuario, "lucia@ejemplo.es", "https://x");
        Func<Task> codigo = () => remitente.SendPasswordResetCodeAsync(usuario, "lucia@ejemplo.es", "123456");

        await confirmacion.Should().ThrowAsync<NotSupportedException>();
        await codigo.Should().ThrowAsync<NotSupportedException>();
        _correo.Enviados.Should().BeEmpty();
    }

    public async ValueTask DisposeAsync()
    {
        await _plantillas.DisposeAsync();
        await _servicios.DisposeAsync();
    }

    private sealed class CorreoCapturado : IServicioEmail
    {
        public List<MensajeCorreo> Enviados { get; } = [];

        public Task EnviarAsync(MensajeCorreo mensaje, CancellationToken ct)
        {
            Enviados.Add(mensaje);
            return Task.CompletedTask;
        }
    }
}
