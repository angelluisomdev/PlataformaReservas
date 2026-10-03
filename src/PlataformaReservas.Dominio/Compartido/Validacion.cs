namespace PlataformaReservas.Dominio.Compartido;

internal static class Validacion
{
    public static void IdentificadorObligatorio(Guid id, string que)
    {
        if (id == Guid.Empty)
        {
            throw new DominioException($"{que} necesita un identificador.");
        }
    }

    public static void TextoObligatorio(string valor, int longitudMaxima, string que)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DominioException($"{que} es obligatorio.");
        }

        if (valor.Trim().Length > longitudMaxima)
        {
            throw new DominioException($"{que} no puede superar {longitudMaxima} caracteres.");
        }
    }

    public static void TextoOpcional(string? valor, int longitudMaxima, string que)
    {
        if (valor is not null && valor.Trim().Length > longitudMaxima)
        {
            throw new DominioException($"{que} no puede superar {longitudMaxima} caracteres.");
        }
    }

    public static void InstanteUtc(DateTime instante, string que)
    {
        if (instante.Kind != DateTimeKind.Utc)
        {
            throw new DominioException($"{que} debe estar en UTC.");
        }
    }
}
