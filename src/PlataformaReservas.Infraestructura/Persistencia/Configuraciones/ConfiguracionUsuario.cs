using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaReservas.Infraestructura.Identidad;

namespace PlataformaReservas.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionUsuario : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("asp_net_users");

        builder.Property(u => u.NombreCompleto).IsRequired();
        builder.Property(u => u.Telefono);
    }
}
