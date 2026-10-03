using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Infraestructura;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Identidad;
using Testcontainers.PostgreSql;

namespace PlataformaReservas.PruebasIntegracion.Infraestructura;

public sealed class BaseDatosFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _contenedor = new PostgreSqlBuilder("postgres:latest")
        .Build();

    private ServiceProvider _servicios = null!;

    public ProtectorFalso Protector { get; } = new();

    public ServiceProvider ServiciosIdentidad { get; private set; } = null!;

    public RemitenteCaptura Remitente { get; } = new();

    public string CadenaConexion => _contenedor.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _contenedor.StartAsync();

        ServiceCollection servicios = new();
        servicios.AddSingleton<IPersonalDataProtector>(Protector);
        _servicios = servicios.BuildServiceProvider();

        // Migraciones, nunca EnsureCreated: no ejecuta el SQL manual y la restriccion EXCLUDE no existiria.
        await using ContextoDatos db = CrearContexto();
        await db.Database.MigrateAsync();

        ServiciosIdentidad = CrearServiciosIdentidad();
    }

    public ContextoDatos CrearContexto()
    {
        DbContextOptions<ContextoDatos> opciones = new DbContextOptionsBuilder<ContextoDatos>()
            .UseNpgsql(CadenaConexion)
            .UseSnakeCaseNamingConvention()
            .UseApplicationServiceProvider(_servicios)
            .EnableServiceProviderCaching(false)
            .Options;

        return new ContextoDatos(opciones);
    }

    private ServiceProvider CrearServiciosIdentidad()
    {
        IConfiguration configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlataformaReservas"] = CadenaConexion,
                ["Cifrado:ClaveDatos"] = ClaveAleatoria(),
                ["Cifrado:ClaveBusqueda"] = ClaveAleatoria(),
            })
            .Build();

        ServiceCollection servicios = new();
        servicios.AddLogging();
        servicios.AgregarInfraestructura(configuracion);
        servicios.AddSingleton<IEmailSender<Usuario>>(Remitente);
        return servicios.BuildServiceProvider();
    }

    public static string ClaveAleatoria() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public async Task DisposeAsync()
    {
        await ServiciosIdentidad.DisposeAsync();
        await _servicios.DisposeAsync();
        await _contenedor.DisposeAsync();
    }
}
