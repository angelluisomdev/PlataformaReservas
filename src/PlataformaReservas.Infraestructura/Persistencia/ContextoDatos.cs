using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia.Configuraciones;

namespace PlataformaReservas.Infraestructura.Persistencia;

public sealed class ContextoDatos(DbContextOptions<ContextoDatos> opciones)
    : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>(opciones)
{
    public DbSet<Categoria> Categorias => Set<Categoria>();

    public DbSet<Empresa> Empresas => Set<Empresa>();

    public DbSet<MiembroEmpresa> MiembrosEmpresa => Set<MiembroEmpresa>();

    public DbSet<Servicio> Servicios => Set<Servicio>();

    public DbSet<Profesional> Profesionales => Set<Profesional>();

    public DbSet<Reserva> Reservas => Set<Reserva>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ContextoDatos).Assembly);
        builder.ApplyConfiguration(new ConfiguracionReserva(ObtenerProtector()));
    }

    // Sin IPersonalDataProtector registrado (dotnet ef, o hasta la Fase 6) el modelo se construye sin conversor.
    private IPersonalDataProtector? ObtenerProtector()
    {
        IServiceProvider? aplicacion = this.GetService<IDbContextOptions>()
            .FindExtension<CoreOptionsExtension>()?
            .ApplicationServiceProvider;

        return aplicacion?.GetService(typeof(IPersonalDataProtector)) as IPersonalDataProtector;
    }
}
