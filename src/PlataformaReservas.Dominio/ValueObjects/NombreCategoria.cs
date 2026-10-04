using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.ValueObjects;

public sealed record NombreCategoria
{
    public const int LongitudMaxima = 80;

    public NombreCategoria(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DominioException("El nombre de la categoria es obligatorio.");
        }

        string recortado = valor.Trim();

        if (recortado.Length > LongitudMaxima)
        {
            throw new DominioException($"El nombre de la categoria no puede superar {LongitudMaxima} caracteres.");
        }

        Valor = recortado;
    }

    public string Valor { get; }

    public override string ToString()
    {
        return Valor;
    }
}
