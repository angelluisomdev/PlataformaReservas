using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.ValueObjects;

public sealed record Observaciones
{
    public const int LongitudMaxima = 500;

    public Observaciones(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DominioException("Las observaciones son obligatorias.");
        }

        string recortado = valor.Trim();

        if (recortado.Length > LongitudMaxima)
        {
            throw new DominioException($"Las observaciones no pueden superar {LongitudMaxima} caracteres.");
        }

        Valor = recortado;
    }

    public string Valor { get; }

    public override string ToString()
    {
        return Valor;
    }
}
