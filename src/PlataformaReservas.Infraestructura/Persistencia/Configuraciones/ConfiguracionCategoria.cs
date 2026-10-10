using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Persistencia.Conversores;

namespace PlataformaReservas.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionCategoria : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("categorias");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nombre).HasConversion<ConversorNombreCategoria>().HasMaxLength(80).IsRequired();
        builder.Property(c => c.Slug)
            .HasConversion(s => s.Valor, v => Slug.Desde(v))
            .HasMaxLength(80)
            .IsRequired();

        builder.HasIndex(c => c.Nombre).IsUnique();
        builder.HasIndex(c => c.Slug).IsUnique();
    }
}
