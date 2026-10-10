using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.ValueObjects;

public sealed record AntelacionMinimaReserva
{
    public const int Minimo = 0, Maximo = 43_200;

    public AntelacionMinimaReserva(int minutos)
    {
        if (minutos < Minimo || minutos > Maximo)
        {
            throw new DominioException($"La antelacion minima debe estar entre {Minimo} y {Maximo} minutos.");
        }

        Minutos = minutos;
    }

    public int Minutos { get; }
}
