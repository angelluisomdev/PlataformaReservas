using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Infraestructura.Identidad;
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

        servicios.AgregarIdentidad(configuracion);

        return servicios;
    }

    private static void AgregarIdentidad(this IServiceCollection servicios, IConfiguration configuracion)
    {
        servicios.AddSingleton(_ => new AnilloClaves(configuracion));
        servicios.AddSingleton<ILookupProtectorKeyRing>(sp => sp.GetRequiredService<AnilloClaves>());
        servicios.AddSingleton<ILookupProtector, ProtectorBusqueda>();
        servicios.AddSingleton<IPersonalDataProtector, ProtectorDatosPersonales>();

        servicios.AddAuthentication(opciones =>
            {
                opciones.DefaultScheme = IdentityConstants.ApplicationScheme;
                opciones.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies();

        servicios.ConfigureApplicationCookie(opciones => opciones.LoginPath = "/login");

        servicios.AddIdentityCore<Usuario>(opciones =>
            {
                opciones.SignIn.RequireConfirmedAccount = true;
                opciones.User.RequireUniqueEmail = true;
                opciones.Password.RequiredLength = 8;
                opciones.Stores.ProtectPersonalData = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ContextoDatos>()
            .AddSignInManager()
            .AddClaimsPrincipalFactory<FabricaClaimsUsuario>()
            .AddUserConfirmation<ConfirmacionUsuarioActivo>()
            .AddDefaultTokenProviders();

        servicios.AddScoped<SolicitudRestablecimientoClave>();

        servicios.AddAuthorizationBuilder()
            .AddPolicy("EsCliente", p => p.RequireRole("Cliente"))
            .AddPolicy("EsPropietario", p => p
                .RequireRole("Propietario")
                .RequireClaim(FabricaClaimsUsuario.ClaimEmpresa));
    }
}
