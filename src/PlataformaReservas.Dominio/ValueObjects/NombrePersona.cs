using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.ValueObjects;

public sealed record NombrePersona
{
    public const int LongitudMaxima = 120;

    public NombrePersona(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DominioException("El nombre es obligatorio.");
        }

        string recortado = valor.Trim();

        if (recortado.Length > LongitudMaxima)
        {
            throw new DominioException($"El nombre no puede superar {LongitudMaxima} caracteres.");
        }

        Valor = recortado;
    }

    public string Valor { get; }

    public override string ToString()
    {
        return Valor;
    }
}
