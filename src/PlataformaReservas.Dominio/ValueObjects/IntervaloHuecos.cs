using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.ValueObjects;

public sealed record IntervaloHuecos
{
    public const int Minimo = 1, Maximo = 480;

    public IntervaloHuecos(int minutos)
    {
        if (minutos < Minimo || minutos > Maximo)
        {
            throw new DominioException($"El intervalo entre huecos debe estar entre {Minimo} y {Maximo} minutos.");
        }

        Minutos = minutos;
    }

    public int Minutos { get; }
}
