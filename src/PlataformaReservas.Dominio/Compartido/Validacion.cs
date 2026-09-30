namespace PlataformaReservas.Dominio.Compartido;

// Comprobaciones de invariantes comunes a las fabricas de las entidades. Todas lanzan DominioException.
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

    // Npgsql rechaza cualquier DateTime no UTC destinado a timestamptz (RN-100, R-2).
    public static void InstanteUtc(DateTime instante, string que)
    {
        if (instante.Kind != DateTimeKind.Utc)
        {
            throw new DominioException($"{que} debe estar en UTC.");
        }
    }
}
