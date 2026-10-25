namespace PlataformaReservas.Infraestructura.Correo;

internal static class EnmascaradoCorreo
{
    public static string Enmascarar(string correo)
    {
        int arroba = correo.IndexOf('@', StringComparison.Ordinal);
        return arroba <= 0 ? "***" : $"{correo[0]}***{correo[arroba..]}";
    }
}
