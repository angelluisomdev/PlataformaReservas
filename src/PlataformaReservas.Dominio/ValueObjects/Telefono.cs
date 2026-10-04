using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.ValueObjects;

public sealed record Telefono
{
    public const int LongitudMaxima = 20;
    public const int DigitosMinimos = 6;
    public const int DigitosMaximos = 15;

    public Telefono(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DominioException("El telefono es obligatorio.");
        }

        string recortado = valor.Trim();

        if (recortado.Length > LongitudMaxima)
        {
            throw new DominioException($"El telefono no puede superar {LongitudMaxima} caracteres.");
        }

        if (!TieneCaracteresValidos(recortado))
        {
            throw new DominioException("El telefono solo admite digitos, espacios y guiones, con un + opcional al principio.");
        }

        int digitos = recortado.Count(char.IsAsciiDigit);
        if (digitos < DigitosMinimos || digitos > DigitosMaximos)
        {
            throw new DominioException($"El telefono debe tener entre {DigitosMinimos} y {DigitosMaximos} digitos.");
        }

        Valor = recortado;
    }

    public string Valor { get; }

    public override string ToString()
    {
        return Valor;
    }

    private static bool TieneCaracteresValidos(string valor)
    {
        string cuerpo = valor.StartsWith('+') ? valor[1..] : valor;
        return cuerpo.All(c => char.IsAsciiDigit(c) || c is ' ' or '-');
    }
}
