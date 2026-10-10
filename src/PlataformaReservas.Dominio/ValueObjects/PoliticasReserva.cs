using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Dominio.ValueObjects;

public sealed record PoliticasReserva
{
    public const int IntervaloMinimo = IntervaloHuecos.Minimo, IntervaloMaximo = IntervaloHuecos.Maximo;
    public const int AntelacionMinima = AntelacionMinimaReserva.Minimo, AntelacionMaxima = AntelacionMinimaReserva.Maximo;
    public const int VentanaMinima = AntelacionMaximaReserva.Minimo, VentanaMaxima = AntelacionMaximaReserva.Maximo;

    public PoliticasReserva(int intervaloHuecosMinutos, int antelacionMinimaMinutos, int antelacionMaximaDias)
    {
        IntervaloHuecosMinutos = new IntervaloHuecos(intervaloHuecosMinutos).Minutos;
        AntelacionMinimaMinutos = new AntelacionMinimaReserva(antelacionMinimaMinutos).Minutos;
        AntelacionMaximaDias = new AntelacionMaximaReserva(antelacionMaximaDias).Dias;
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
