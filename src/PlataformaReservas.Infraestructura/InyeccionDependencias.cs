using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.Infraestructura;

public static class InyeccionDependencias
{
    public static IServiceCollection AgregarInfraestructura(this IServiceCollection servicios, IConfiguration configuracion)
    {
        // La cadena sale de User Secrets en local y de variables de entorno en Docker; nunca de appsettings.json (§11.3).
        string? cadenaConexion = configuracion.GetConnectionString("PlataformaReservas");

        servicios.AddDbContextFactory<ContextoDatos>(opciones => opciones
            .UseNpgsql(cadenaConexion)
            .UseSnakeCaseNamingConvention()
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        // Puente para Identity y para los repositorios: un contexto por ambito, creado por la fabrica (A-04).
        servicios.AddScoped<ContextoDatos>(sp =>
            sp.GetRequiredService<IDbContextFactory<ContextoDatos>>().CreateDbContext());

        return servicios;
    }
}
