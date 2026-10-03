using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Infraestructura.Persistencia;
using Testcontainers.PostgreSql;

namespace PlataformaReservas.PruebasIntegracion.Infraestructura;

public sealed class BaseDatosFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _contenedor = new PostgreSqlBuilder("postgres:latest")
        .Build();

    private ServiceProvider _servicios = null!;

    public ProtectorFalso Protector { get; } = new();

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
