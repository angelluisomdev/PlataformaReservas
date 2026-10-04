using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.ValueObjects;

public sealed record DescripcionEmpresa
{
    public const int LongitudMaxima = 1000;

    public DescripcionEmpresa(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DominioException("La descripcion de la empresa es obligatoria.");
        }

        string recortado = valor.Trim();

        if (recortado.Length > LongitudMaxima)
        {
            throw new DominioException($"La descripcion de la empresa no puede superar {LongitudMaxima} caracteres.");
        }

        Valor = recortado;
    }

    public string Valor { get; }

    public override string ToString()
    {
        return Valor;
    }
}
