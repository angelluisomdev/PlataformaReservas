using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Dominio.ValueObjects;

// Los tres parametros efectivos de reserva (SPEC §4.1).
// Los rangos (RN-26 a RN-29) se comprueban en el constructor a partir de la Fase 10.
public sealed record PoliticasReserva
{
    public const int IntervaloMinimo = 1, IntervaloMaximo = 480;    // minutos
    public const int AntelacionMinima = 0, AntelacionMaxima = 43_200; // minutos (30 dias)
    public const int VentanaMinima = 1, VentanaMaxima = 365;       // dias

    public PoliticasReserva(int intervaloHuecosMinutos, int antelacionMinimaMinutos, int antelacionMaximaDias)
    {
        IntervaloHuecosMinutos = intervaloHuecosMinutos;
        AntelacionMinimaMinutos = antelacionMinimaMinutos;
        AntelacionMaximaDias = antelacionMaximaDias;
    }

    public int IntervaloHuecosMinutos { get; }

    public int AntelacionMinimaMinutos { get; }

    public int AntelacionMaximaDias { get; }

    // Valor efectivo = Servicio.X ?? Empresa.X. Se implementa una sola vez (RN-23).
    public static PoliticasReserva Resolver(Empresa empresa, Servicio servicio)
    {
        return new PoliticasReserva(
            servicio.IntervaloHuecosMinutos ?? empresa.Politicas.IntervaloHuecosMinutos,
            servicio.AntelacionMinimaMinutos ?? empresa.Politicas.AntelacionMinimaMinutos,
            servicio.AntelacionMaximaDias ?? empresa.Politicas.AntelacionMaximaDias);
    }
}
