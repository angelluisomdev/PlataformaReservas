using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.ValueObjects;

public sealed record Ciudad
{
    public const int LongitudMaxima = 80;

    public Ciudad(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DominioException("La ciudad es obligatoria.");
        }

        string recortado = valor.Trim();

        if (recortado.Length > LongitudMaxima)
        {
            throw new DominioException($"La ciudad no puede superar {LongitudMaxima} caracteres.");
        }

        Valor = recortado;
    }

    public string Valor { get; }

    public override string ToString()
    {
        return Valor;
    }
}
