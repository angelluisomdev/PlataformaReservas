using System.Globalization;
using System.Text;
using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.ValueObjects;

// Aparece en la URL publica /businesses/{slug}: un slug mal formado rompe el enrutado (modelo-dominio.md §5.4).
public sealed record Slug
{
    public const int LongitudMinima = 3;
    public const int LongitudMaxima = 120;

    private Slug(string valor)
    {
        if (!EsValido(valor))
        {
            throw new DominioException(
                "El slug solo admite minusculas, digitos y guiones, sin guiones al principio, al final ni dobles, "
                + $"y debe tener entre {LongitudMinima} y {LongitudMaxima} caracteres.");
        }

        Valor = valor;
    }

    public string Valor { get; }

    // Normaliza: quita tildes, pasa a minusculas y sustituye cualquier otro caracter por un guion.
    public static Slug Desde(string texto)
    {
        string descompuesto = texto.Normalize(NormalizationForm.FormD);
        StringBuilder resultado = new(descompuesto.Length);
        bool guionPendiente = false;

        foreach (char caracter in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            char minuscula = char.ToLowerInvariant(caracter);
            bool admitido = minuscula is (>= 'a' and <= 'z') or (>= '0' and <= '9');

            if (!admitido)
            {
                guionPendiente = resultado.Length > 0;
                continue;
            }

            if (guionPendiente)
            {
                resultado.Append('-');
                guionPendiente = false;
            }

            resultado.Append(minuscula);
        }

        string valor = resultado.ToString();
        if (valor.Length > LongitudMaxima)
        {
            valor = valor[..LongitudMaxima].TrimEnd('-');
        }

        return new Slug(valor);
    }

    // Resuelve colisiones anadiendo un sufijo numerico (RN-11).
    public Slug ConSufijo(int n)
    {
        string sufijo = "-" + n.ToString(CultureInfo.InvariantCulture);
        string baseSlug = Valor;
        if (baseSlug.Length + sufijo.Length > LongitudMaxima)
        {
            baseSlug = baseSlug[..(LongitudMaxima - sufijo.Length)].TrimEnd('-');
        }

        return new Slug(baseSlug + sufijo);
    }

    public override string ToString()
    {
        return Valor;
    }

    private static bool EsValido(string valor)
    {
        if (valor.Length < LongitudMinima || valor.Length > LongitudMaxima)
        {
            return false;
        }

        if (valor[0] == '-' || valor[^1] == '-' || valor.Contains("--", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (char caracter in valor)
        {
            bool admitido = caracter is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-';
            if (!admitido)
            {
                return false;
            }
        }

        return true;
    }
}
