namespace PlataformaReservas.Web;

public static class CabecerasSeguridad
{
    public const string PoliticaContenido =
        "default-src 'self'; " +
        "script-src 'self' 'wasm-unsafe-eval'; " +
        "style-src 'self'; img-src 'self' data:; " +
        "connect-src 'self'; frame-ancestors 'none'; base-uri 'self'";

    public static IApplicationBuilder UsarCabecerasSeguridad(this IApplicationBuilder aplicacion)
    {
        return aplicacion.Use(async (contexto, siguiente) =>
        {
            Agregar(contexto.Response.Headers);
            await siguiente(contexto);
        });
    }

    public static void Agregar(IHeaderDictionary cabeceras)
    {
        ArgumentNullException.ThrowIfNull(cabeceras);

        cabeceras.XContentTypeOptions = "nosniff";
        cabeceras.XFrameOptions = "DENY";
        cabeceras["Referrer-Policy"] = "strict-origin-when-cross-origin";
        cabeceras.ContentSecurityPolicy = PoliticaContenido;
    }
}
