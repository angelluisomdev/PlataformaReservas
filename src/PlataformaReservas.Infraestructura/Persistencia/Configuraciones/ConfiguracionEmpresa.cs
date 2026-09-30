using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Infraestructura.Persistencia.Configuraciones;

// Los datos de la empresa NO se cifran: son publicos y Ciudad es el filtro del buscador (RN-164).
public sealed class ConfiguracionEmpresa : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        builder.ToTable("empresas");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Nombre).HasMaxLength(120).IsRequired();
        builder.Property(e => e.Slug)
            .HasConversion(s => s.Valor, v => Slug.Desde(v))
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(e => e.Descripcion).HasMaxLength(1000);
        builder.Property(e => e.Email).HasMaxLength(160).IsRequired();
        builder.Property(e => e.Telefono).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Direccion).HasMaxLength(200).IsRequired();
        builder.Property(e => e.CodigoPostal).HasMaxLength(10).IsRequired();
        builder.Property(e => e.Ciudad).HasMaxLength(80).IsRequired();
        builder.Property(e => e.ZonaHoraria).HasMaxLength(60).IsRequired();

        // El objeto de valor se aplana en tres columnas de la propia tabla (modelo-dominio.md §8).
        builder.ComplexProperty(e => e.Politicas, politicas =>
        {
            politicas.Property(p => p.IntervaloHuecosMinutos).HasColumnName("intervalo_huecos_minutos");
            politicas.Property(p => p.AntelacionMinimaMinutos).HasColumnName("antelacion_minima_minutos");
            politicas.Property(p => p.AntelacionMaximaDias).HasColumnName("antelacion_maxima_dias");
        });

        builder.HasOne<Categoria>()
            .WithMany()
            .HasForeignKey(e => e.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.Slug).IsUnique();
        builder.HasIndex(e => new { e.Ciudad, e.CategoriaId });   // consulta del buscador
        builder.HasIndex(e => e.Activa);
    }
}
