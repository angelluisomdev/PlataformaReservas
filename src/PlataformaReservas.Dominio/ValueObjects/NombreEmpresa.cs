using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.ValueObjects;

public sealed record NombreEmpresa
{
    public const int LongitudMaxima = 120;

    public NombreEmpresa(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DominioException("El nombre de la empresa es obligatorio.");
        }

        string recortado = valor.Trim();

        if (recortado.Length > LongitudMaxima)
        {
            throw new DominioException($"El nombre de la empresa no puede superar {LongitudMaxima} caracteres.");
        }

        Valor = recortado;
    }

    public string Valor { get; }

    public override string ToString()
    {
        return Valor;
    }
}
