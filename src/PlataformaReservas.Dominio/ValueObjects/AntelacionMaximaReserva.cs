using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.ValueObjects;

public sealed record AntelacionMaximaReserva
{
    public const int Minimo = 1, Maximo = 365;

    public AntelacionMaximaReserva(int dias)
    {
        if (dias < Minimo || dias > Maximo)
        {
            throw new DominioException($"La antelacion maxima debe estar entre {Minimo} y {Maximo} dias.");
        }

        Dias = dias;
    }

    public int Dias { get; }
}
