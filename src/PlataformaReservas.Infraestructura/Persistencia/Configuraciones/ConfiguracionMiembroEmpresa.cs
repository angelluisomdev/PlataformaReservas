using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Infraestructura.Identidad;

namespace PlataformaReservas.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionMiembroEmpresa : IEntityTypeConfiguration<MiembroEmpresa>
{
    public void Configure(EntityTypeBuilder<MiembroEmpresa> builder)
    {
        builder.ToTable("miembros_empresa");
        builder.HasKey(m => m.Id);

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(m => m.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(m => m.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Una pertenencia por usuario y empresa (RN-06).
        builder.HasIndex(m => new { m.EmpresaId, m.UsuarioId }).IsUnique();
    }
}
