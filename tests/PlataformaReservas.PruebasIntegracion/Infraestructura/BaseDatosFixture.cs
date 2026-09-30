using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Infraestructura.Persistencia;
using Testcontainers.PostgreSql;

namespace PlataformaReservas.PruebasIntegracion.Infraestructura;

// Un contenedor de PostgreSQL real para toda la suite (A-10, arquitectura.md §12.2).
public sealed class BaseDatosFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _contenedor = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    private ServiceProvider _servicios = null!;

    public ProtectorFalso Protector { get; } = new();

    public string CadenaConexion => _contenedor.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _contenedor.StartAsync();

        // El contexto obtiene el protector de los servicios de la aplicacion, como en produccion.
        ServiceCollection servicios = new();
        servicios.AddSingleton<IPersonalDataProtector>(Protector);
        _servicios = servicios.BuildServiceProvider();

        // Se aplican las MIGRACIONES, nunca EnsureCreated: EnsureCreated no ejecuta el SQL manual de la
        // migracion, asi que la restriccion EXCLUDE no existiria y la prueba de concurrencia pasaria
        // sin probar nada (R-3, arquitectura.md §12.2).
        await using ContextoDatos db = CrearContexto();
        await db.Database.MigrateAsync();
    }

    public ContextoDatos CrearContexto()
    {
        DbContextOptions<ContextoDatos> opciones = new DbContextOptionsBuilder<ContextoDatos>()
            .UseNpgsql(CadenaConexion)
            .UseSnakeCaseNamingConvention()
            .UseApplicationServiceProvider(_servicios)
            .Options;

        return new ContextoDatos(opciones);
    }

    public async Task DisposeAsync()
    {
        await _servicios.DisposeAsync();
        await _contenedor.DisposeAsync();
    }
}
