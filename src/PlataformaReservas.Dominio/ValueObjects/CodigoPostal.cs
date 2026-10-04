using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.ValueObjects;

public sealed record CodigoPostal
{
    public const int Longitud = 5;

    public CodigoPostal(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DominioException("El codigo postal es obligatorio.");
        }

        string recortado = valor.Trim();

        if (recortado.Length != Longitud || !recortado.All(char.IsAsciiDigit))
        {
            throw new DominioException($"El codigo postal debe tener exactamente {Longitud} digitos.");
        }

        Valor = recortado;
    }

    public string Valor { get; }

    public override string ToString()
    {
        return Valor;
    }
}
