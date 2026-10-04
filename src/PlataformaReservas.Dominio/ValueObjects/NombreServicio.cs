using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.ValueObjects;

public sealed record NombreServicio
{
    public const int LongitudMaxima = 120;

    public NombreServicio(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DominioException("El nombre del servicio es obligatorio.");
        }

        string recortado = valor.Trim();

        if (recortado.Length > LongitudMaxima)
        {
            throw new DominioException($"El nombre del servicio no puede superar {LongitudMaxima} caracteres.");
        }

        Valor = recortado;
    }

    public string Valor { get; }

    public override string ToString()
    {
        return Valor;
    }
}
