using FluentAssertions;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Notificaciones;
using PlataformaReservas.Dominio.Eventos;
using PlataformaReservas.PruebasUnitarias.Infraestructura;

namespace PlataformaReservas.PruebasUnitarias.Notificaciones;

public sealed class PruebasEnviarCorreoEmpresaRegistrada
{
    private static readonly DateTime Ahora = new(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Envia_el_alta_al_propietario()
    {
        CorreoCaptura correo = new();
        ConsultasNotificacionFalsas consultas = new() { Empresa = ConsultasNotificacionFalsas.UnaEmpresa() };

        await new EnviarCorreoEmpresaRegistrada(consultas, new RenderizadorFalso(), correo)
            .ManejarAsync(new EmpresaRegistrada(Guid.CreateVersion7(), Ahora), CancellationToken.None);

        correo.Enviados.Should().Equal(new MensajeCorreo("ana@barberia.es", nameof(IRenderizadorPlantillas.EmpresaRegistradaAsync), ""));
        consultas.InstantesPedidos.Should().Equal(Ahora);
    }

    [Fact]
    public async Task Si_la_empresa_no_existe_no_envia_nada()
    {
        CorreoCaptura correo = new();

        await new EnviarCorreoEmpresaRegistrada(new ConsultasNotificacionFalsas(), new RenderizadorFalso(), correo)
            .ManejarAsync(new EmpresaRegistrada(Guid.CreateVersion7(), Ahora), CancellationToken.None);

        correo.Enviados.Should().BeEmpty();
    }
}
