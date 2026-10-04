using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionServicio : IEntityTypeConfiguration<Servicio>
{
    public void Configure(EntityTypeBuilder<Servicio> builder)
    {
        builder.ToTable("servicios", tabla =>
        {
            tabla.HasCheckConstraint("ck_servicios_duracion", "duracion_minutos > 0");
            tabla.HasCheckConstraint("ck_servicios_precio", "precio >= 0");
        });
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Nombre).HasConversion<ConversorNombreServicio>().HasMaxLength(120).IsRequired();
        builder.Property(s => s.Descripcion).HasMaxLength(500);
        builder.Property(s => s.Duracion).HasConversion<ConversorDuracion>().HasColumnName("duracion_minutos");
        builder.Property(s => s.Precio).HasConversion<ConversorPrecio>().HasPrecision(10, 2);

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(s => s.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.EmpresaId);
    }
}
