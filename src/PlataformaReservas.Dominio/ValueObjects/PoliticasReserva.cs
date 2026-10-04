using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Dominio.ValueObjects;

public sealed record PoliticasReserva
{
    public const int IntervaloMinimo = 1, IntervaloMaximo = 480;
    public const int AntelacionMinima = 0, AntelacionMaxima = 43_200;
    public const int VentanaMinima = 1, VentanaMaxima = 365;

    public PoliticasReserva(int intervaloHuecosMinutos, int antelacionMinimaMinutos, int antelacionMaximaDias)
    {
        if (intervaloHuecosMinutos < IntervaloMinimo || intervaloHuecosMinutos > IntervaloMaximo)
        {
            throw new DominioException(
                $"El intervalo entre huecos debe estar entre {IntervaloMinimo} y {IntervaloMaximo} minutos.");
        }

        if (antelacionMinimaMinutos < AntelacionMinima || antelacionMinimaMinutos > AntelacionMaxima)
        {
            throw new DominioException(
                $"La antelacion minima debe estar entre {AntelacionMinima} y {AntelacionMaxima} minutos.");
        }

        if (antelacionMaximaDias < VentanaMinima || antelacionMaximaDias > VentanaMaxima)
        {
            throw new DominioException(
                $"La antelacion maxima debe estar entre {VentanaMinima} y {VentanaMaxima} dias.");
        }

        IntervaloHuecosMinutos = intervaloHuecosMinutos;
        AntelacionMinimaMinutos = antelacionMinimaMinutos;
        AntelacionMaximaDias = antelacionMaximaDias;
    }

    public int IntervaloHuecosMinutos { get; }

    public int AntelacionMinimaMinutos { get; }

    public int AntelacionMaximaDias { get; }

    public static PoliticasReserva Resolver(Empresa empresa, Servicio servicio)
    {
        return new PoliticasReserva(
            servicio.IntervaloHuecosMinutos ?? empresa.Politicas.IntervaloHuecosMinutos,
            servicio.AntelacionMinimaMinutos ?? empresa.Politicas.AntelacionMinimaMinutos,
            servicio.AntelacionMaximaDias ?? empresa.Politicas.AntelacionMaximaDias);
    }
}
