using FluentAssertions;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.Eventos;
using PlataformaReservas.Infraestructura.Eventos;

namespace PlataformaReservas.PruebasIntegracion.Eventos;

public sealed class PruebasColaEventos
{
    private static readonly DateTime Ahora = new(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Publicar_sin_lector_termina_enseguida_y_los_eventos_se_leen_despues_en_orden()
    {
        ColaEventos cola = new();
        IEventoDominio[] eventos =
        [
            new EmpresaDadaDeBaja(Guid.CreateVersion7(), Ahora),
            new ReservaCancelada(Guid.CreateVersion7(), OrigenCancelacion.BajaEmpresa, Ahora),
            new ReservaCancelada(Guid.CreateVersion7(), OrigenCancelacion.BajaEmpresa, Ahora),
        ];

        ValueTask publicacion = cola.PublicarAsync(eventos, CancellationToken.None);

        publicacion.IsCompletedSuccessfully.Should().BeTrue();
        (await LeerAsync(cola, eventos.Length)).Should().Equal(eventos);
    }

    [Fact]
    public async Task Publicar_una_coleccion_vacia_no_encola_nada()
    {
        ColaEventos cola = new();
        ReservaConfirmada posterior = new(Guid.CreateVersion7(), Ahora);

        await cola.PublicarAsync([], CancellationToken.None);
        await cola.PublicarAsync([posterior], CancellationToken.None);

        (await LeerAsync(cola, 1)).Should().Equal(posterior);
    }

    private static async Task<List<IEventoDominio>> LeerAsync(ColaEventos cola, int cuantos)
    {
        using CancellationTokenSource limite = new(TimeSpan.FromSeconds(5));
        List<IEventoDominio> leidos = [];
        await foreach (IEventoDominio evento in cola.LeerAsync(limite.Token))
        {
            leidos.Add(evento);
            if (leidos.Count == cuantos)
            {
                break;
            }
        }

        return leidos;
    }
}
