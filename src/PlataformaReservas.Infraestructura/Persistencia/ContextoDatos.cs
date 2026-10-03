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

    // Falla en cerrado: sin protector, las copias de Reserva se guardarian en claro. Solo dotnet ef puede prescindir de el.
    private IPersonalDataProtector? ObtenerProtector()
    {
        IServiceProvider? aplicacion = this.GetService<IDbContextOptions>()
            .FindExtension<CoreOptionsExtension>()?
            .ApplicationServiceProvider;

        IPersonalDataProtector? protector = aplicacion?.GetService(typeof(IPersonalDataProtector)) as IPersonalDataProtector;
        if (protector is null && !EF.IsDesignTime)
        {
            throw new InvalidOperationException(
                "No hay ningun IPersonalDataProtector registrado: los datos personales de las reservas se guardarian en claro.");
        }

        return protector;
    }
}
