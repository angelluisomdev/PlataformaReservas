using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia.Configuraciones;

namespace PlataformaReservas.Infraestructura.Persistencia;

// Sin filtros globales de consulta: el filtrado por empresa se hace en origen, en cada repositorio (SPEC §10.2).
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
        base.OnModelCreating(builder);      // imprescindible: configura Identity

        // Aplica todas las configuraciones con constructor sin parametros.
        // ConfiguracionReserva no lo tiene y se aplica aparte, con el protector.
        builder.ApplyConfigurationsFromAssembly(typeof(ContextoDatos).Assembly);
        builder.ApplyConfiguration(new ConfiguracionReserva(ObtenerProtector()));
    }

    // Hasta la Fase 6 no hay ningun IPersonalDataProtector registrado, ni en la aplicacion ni al
    // generar migraciones: en ese caso el modelo se construye sin conversor. El esquema no cambia
    // (las columnas son text en ambos casos). Decision 1 de T4-T12 en docs/fases/fase-05.md; ver R-11.
    private IPersonalDataProtector? ObtenerProtector()
    {
        IServiceProvider? aplicacion = this.GetService<IDbContextOptions>()
            .FindExtension<CoreOptionsExtension>()?
            .ApplicationServiceProvider;

        return aplicacion?.GetService(typeof(IPersonalDataProtector)) as IPersonalDataProtector;
    }
}
