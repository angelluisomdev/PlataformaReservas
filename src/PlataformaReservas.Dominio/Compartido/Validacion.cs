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

    public static void InstanteUtc(DateTime instante, string que)
    {
        if (instante.Kind != DateTimeKind.Utc)
        {
            throw new DominioException($"{que} debe estar en UTC.");
        }
    }
}
