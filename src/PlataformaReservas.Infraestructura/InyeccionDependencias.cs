using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Dominio.Repositorios;
using PlataformaReservas.Infraestructura.Consultas;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.Infraestructura.Repositorios;

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
        servicios.AgregarRepositorios();

        return servicios;
    }

    public static void ComprobarClavesCifrado(this IServiceProvider servicios)
    {
        servicios.GetRequiredService<AnilloClaves>();
    }

    private static void AgregarRepositorios(this IServiceCollection servicios)
    {
        servicios.AddSingleton<IRelojSistema, RelojSistema>();
        servicios.AddScoped<IUnidadTrabajo, UnidadTrabajo>();
        servicios.AddScoped<IConsultasCatalogoPublico, ConsultasCatalogoPublico>();
        servicios.AddScoped<IContextoEmpresa, ContextoEmpresa>();
        servicios.AddScoped<IRepositorioEmpresas, RepositorioEmpresas>();
        servicios.AddScoped<IRepositorioServicios, RepositorioServicios>();
        servicios.AddScoped<IRepositorioProfesionales, RepositorioProfesionales>();
        servicios.AddScoped<IRepositorioReservas, RepositorioReservas>();
        servicios.AddScoped<IRepositorioMiembros, RepositorioMiembros>();
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
        servicios.Configure<SecurityStampValidatorOptions>(opciones =>
            opciones.ValidationInterval = RevalidadorIdentidad.IntervaloRevalidacion);

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
        servicios.AddScoped<RegistroCliente>();
        servicios.AddScoped<IServicioCuentas, ServicioCuentas>();

        servicios.AddAuthorizationBuilder()
            .AddPolicy(RolesIdentidad.PoliticaCliente, p => p.RequireRole(RolesIdentidad.Cliente))
            .AddPolicy(RolesIdentidad.PoliticaPropietario, p => p
                .RequireRole(RolesIdentidad.Propietario)
                .RequireClaim(FabricaClaimsUsuario.ClaimEmpresa));
    }
}
