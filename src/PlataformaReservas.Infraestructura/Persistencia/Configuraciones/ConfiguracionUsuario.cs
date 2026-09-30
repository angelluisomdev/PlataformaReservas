using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaReservas.Infraestructura.Identidad;

namespace PlataformaReservas.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionUsuario : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        // IdentityDbContext fija el nombre "AspNetUsers" y la convencion snake_case no lo reescribe.
        // Se fija aqui para que la tabla sea asp_net_users, como esperan modelo-dominio.md §8 y RN-162.
        builder.ToTable("asp_net_users");

        // Sin longitud maxima en los campos protegidos: cuando se cifren (Fase 6), el texto crece.
        builder.Property(u => u.NombreCompleto).IsRequired();
        builder.Property(u => u.Telefono);
    }
}
