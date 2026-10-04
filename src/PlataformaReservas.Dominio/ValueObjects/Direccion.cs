using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.ValueObjects;

public sealed record Direccion
{
    public const int LongitudMaxima = 200;

    public Direccion(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DominioException("La direccion es obligatoria.");
        }

        string recortado = valor.Trim();

        if (recortado.Length > LongitudMaxima)
        {
            throw new DominioException($"La direccion no puede superar {LongitudMaxima} caracteres.");
        }

        Valor = recortado;
    }

    public string Valor { get; }

    public override string ToString()
    {
        return Valor;
    }
}
