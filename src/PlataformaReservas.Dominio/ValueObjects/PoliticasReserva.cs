using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Dominio.ValueObjects;

public sealed record PoliticasReserva
{
    public const int IntervaloMinimo = 1, IntervaloMaximo = 480;
    public const int AntelacionMinima = 0, AntelacionMaxima = 43_200;
    public const int VentanaMinima = 1, VentanaMaxima = 365;

    public PoliticasReserva(int intervaloHuecosMinutos, int antelacionMinimaMinutos, int antelacionMaximaDias)
    {
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
