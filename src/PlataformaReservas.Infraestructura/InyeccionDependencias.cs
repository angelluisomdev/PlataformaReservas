using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.Infraestructura;

public static class InyeccionDependencias
{
    public static IServiceCollection AgregarInfraestructura(this IServiceCollection servicios, IConfiguration configuracion)
    {
        string? cadenaConexion = configuracion.GetConnectionString("PlataformaReservas");

        servicios.AddDbContextFactory<ContextoDatos>(opciones => opciones
            .UseNpgsql(cadenaConexion)
            .UseSnakeCaseNamingConvention()
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        servicios.AddScoped<ContextoDatos>(sp =>
            sp.GetRequiredService<IDbContextFactory<ContextoDatos>>().CreateDbContext());

        return servicios;
    }
}
