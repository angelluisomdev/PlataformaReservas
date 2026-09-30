using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionProfesional : IEntityTypeConfiguration<Profesional>
{
    public void Configure(EntityTypeBuilder<Profesional> builder)
    {
        builder.ToTable("profesionales");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.NombreCompleto).HasMaxLength(120).IsRequired();
        builder.Property(p => p.Email).HasMaxLength(160);
        builder.Property(p => p.Telefono).HasMaxLength(20);

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(p => p.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Hijos del agregado: se cargan, validan y guardan con el profesional (modelo-dominio.md §2.1).
        builder.HasMany(p => p.Horarios)
            .WithOne()
            .HasForeignKey(h => h.ProfesionalId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Horarios).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(p => p.Excepciones)
            .WithOne()
            .HasForeignKey(x => x.ProfesionalId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Excepciones).UsePropertyAccessMode(PropertyAccessMode.Field);

        // En el dominio es un conjunto de Guid; en base de datos, la tabla de union profesional_servicio
        // con clave compuesta, que garantiza la unicidad del par (RN-32).
        builder.Ignore(p => p.ServiciosQuePresta);
        builder.OwnsMany<ProfesionalServicio>("_servicios", servicio =>
        {
            servicio.ToTable("profesional_servicio");
            servicio.WithOwner().HasForeignKey("ProfesionalId");
            servicio.Property<Guid>("ProfesionalId");
            servicio.HasKey("ProfesionalId", nameof(ProfesionalServicio.ServicioId));
            servicio.HasOne<Servicio>()
                .WithMany()
                .HasForeignKey(ps => ps.ServicioId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Navigation("_servicios").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(p => p.EmpresaId);
    }
}
