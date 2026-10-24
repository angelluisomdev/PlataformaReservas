using Microsoft.Extensions.Configuration;

namespace PlataformaReservas.Infraestructura.Correo;

public static class UrlBaseCorreo
{
    public const string Clave = "Correo:UrlBase";

    public static bool IntentarObtener(IConfiguration configuracion, out string urlBase)
    {
        ArgumentNullException.ThrowIfNull(configuracion);
        urlBase = (configuracion[Clave] ?? "").TrimEnd('/');
        return Uri.TryCreate(urlBase, UriKind.Absolute, out Uri? raiz)
            && (raiz.Scheme == Uri.UriSchemeHttps || raiz.Scheme == Uri.UriSchemeHttp);
    }
}
