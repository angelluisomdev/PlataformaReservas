#nullable enable

using Microsoft.AspNetCore.Components;

namespace PlataformaReservas.Web.Cuenta;

internal sealed class IdentityRedirectManager(NavigationManager navigationManager)
{
    public const string StatusCookieName = "Identity.StatusMessage";

    private static readonly CookieBuilder StatusCookieBuilder = new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromSeconds(5),
    };

    public void RedirectTo(string? uri)
    {
        navigationManager.NavigateTo(DestinoLocal((uri ?? "").Trim()));
    }

    public void RedirectTo(string uri, Dictionary<string, object?> queryParameters)
    {
        var uriWithoutQuery = navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
        var newUri = navigationManager.GetUriWithQueryParameters(uriWithoutQuery, queryParameters);
        RedirectTo(newUri);
    }

    public void RedirectToWithStatus(string uri, string message, HttpContext context)
    {
        context.Response.Cookies.Append(StatusCookieName, message, StatusCookieBuilder.Build(context));
        RedirectTo(uri);
    }

    // H-02: el navegador trata "//host" y "/\host" como otro dominio; solo se redirige dentro de la aplicacion.
    // Una URI absoluta propia se reduce a su ruta y se valida igual: "https://host///evil.com" daria "//evil.com".
    private string DestinoLocal(string uri)
    {
        if (uri.Any(char.IsControl))
        {
            return "/";
        }

        if (!uri.StartsWith('/') && Uri.TryCreate(uri, UriKind.Absolute, out Uri? absoluta))
        {
            if (!new Uri(navigationManager.BaseUri).IsBaseOf(absoluta))
            {
                return "/";
            }

            uri = absoluta.PathAndQuery + absoluta.Fragment;
        }

        return EsRutaLocal(uri) ? uri : "/";
    }

    private static bool EsRutaLocal(string ruta)
    {
        return !ruta.StartsWith("//", StringComparison.Ordinal)
            && !ruta.StartsWith("/\\", StringComparison.Ordinal)
            && !ruta.StartsWith('\\');
    }

    private string CurrentPath => navigationManager.ToAbsoluteUri(navigationManager.Uri).GetLeftPart(UriPartial.Path);

    public void RedirectToCurrentPage() => RedirectTo(CurrentPath);

    public void RedirectToCurrentPageWithStatus(string message, HttpContext context)
        => RedirectToWithStatus(CurrentPath, message, context);
}
