using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.ValueObjects;

public sealed record Duracion
{
    public Duracion(int minutos)
    {
        if (minutos <= 0)
        {
            throw new DominioException("La duracion debe ser mayor que cero.");
        }

        Minutos = minutos;
    }

    public int Minutos { get; }
}
