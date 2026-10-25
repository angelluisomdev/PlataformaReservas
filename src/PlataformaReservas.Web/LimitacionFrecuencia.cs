using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;

namespace PlataformaReservas.Web;

public static class LimitacionFrecuencia
{
    public const string GrupoAutenticacion = "autenticacion";
    public const string GrupoCatalogo = "catalogo";

    public const int IntentosAutenticacion = 5;
    public const int PeticionesCatalogo = 60;

    public static readonly TimeSpan VentanaAutenticacion = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan VentanaCatalogo = TimeSpan.FromMinutes(1);

    private static readonly string[] RutasAutenticacion =
        ["/login", "/Account/Login", "/register", "/Account/Register", "/Account/ForgotPassword", "/for-business/register"];

    public static IServiceCollection AgregarLimitacionFrecuencia(this IServiceCollection servicios)
    {
        return servicios.AddRateLimiter(opciones =>
        {
            opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opciones.GlobalLimiter = CrearLimitador();
            opciones.OnRejected = (rechazo, ct) => ResponderRechazoAsync(rechazo.HttpContext, ct);
        });
    }

    // Sin esto, UseStatusCodePagesWithReExecute reejecuta el POST rechazado contra /not-found y el 429 acaba en 400 por antiforgery.
    public static async ValueTask ResponderRechazoAsync(HttpContext contexto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        IStatusCodePagesFeature? paginasEstado = contexto.Features.Get<IStatusCodePagesFeature>();
        if (paginasEstado is not null)
        {
            paginasEstado.Enabled = false;
        }

        contexto.Response.ContentType = "text/plain; charset=utf-8";
        await contexto.Response.WriteAsync("Demasiadas peticiones. Espera unos minutos y vuelve a intentarlo.", ct);
    }

    public static PartitionedRateLimiter<HttpContext> CrearLimitador()
    {
        return PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
        {
            string? grupo = GrupoDe(contexto.Request);
            string clave = $"{grupo}|{contexto.Connection.RemoteIpAddress}";

            return grupo switch
            {
                GrupoAutenticacion => RateLimitPartition.GetFixedWindowLimiter(clave, _ => Ventana(IntentosAutenticacion, VentanaAutenticacion)),
                GrupoCatalogo => RateLimitPartition.GetFixedWindowLimiter(clave, _ => Ventana(PeticionesCatalogo, VentanaCatalogo)),
                _ => RateLimitPartition.GetNoLimiter(""),
            };
        });
    }

    // Fase 19, duda 2: en autenticacion cuentan los envios del formulario, no las visitas a la pagina.
    public static string? GrupoDe(HttpRequest peticion)
    {
        ArgumentNullException.ThrowIfNull(peticion);

        string ruta = (peticion.Path.Value ?? "").TrimEnd('/');
        if (HttpMethods.IsPost(peticion.Method)
            && RutasAutenticacion.Any(r => string.Equals(r, ruta, StringComparison.OrdinalIgnoreCase)))
        {
            return GrupoAutenticacion;
        }

        return peticion.Path.StartsWithSegments("/businesses", StringComparison.OrdinalIgnoreCase) ? GrupoCatalogo : null;
    }

    private static FixedWindowRateLimiterOptions Ventana(int limite, TimeSpan ventana)
    {
        return new FixedWindowRateLimiterOptions { PermitLimit = limite, Window = ventana, QueueLimit = 0 };
    }
}
