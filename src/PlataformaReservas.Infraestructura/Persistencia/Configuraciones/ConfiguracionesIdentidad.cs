using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PlataformaReservas.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionRol : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder)
    {
        builder.ToTable("asp_net_roles");

        builder.HasData(
            new IdentityRole<Guid>
            {
                Id = new Guid("0199a3c0-0000-7000-8000-000000000001"),
                Name = "Cliente",
                NormalizedName = "CLIENTE",
                ConcurrencyStamp = "0199a3c0-0000-7000-8000-000000000001",
            },
            new IdentityRole<Guid>
            {
                Id = new Guid("0199a3c0-0000-7000-8000-000000000002"),
                Name = "Propietario",
                NormalizedName = "PROPIETARIO",
                ConcurrencyStamp = "0199a3c0-0000-7000-8000-000000000002",
            });
    }
}

public sealed class ConfiguracionReclamacionRol : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> builder)
    {
        builder.ToTable("asp_net_role_claims");
    }
}

public sealed class ConfiguracionRolUsuario : IEntityTypeConfiguration<IdentityUserRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> builder)
    {
        builder.ToTable("asp_net_user_roles");
    }
}

public sealed class ConfiguracionReclamacionUsuario : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder)
    {
        builder.ToTable("asp_net_user_claims");
    }
}

public sealed class ConfiguracionInicioSesionUsuario : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder)
    {
        builder.ToTable("asp_net_user_logins");
    }
}

public sealed class ConfiguracionTokenUsuario : IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder)
    {
        builder.ToTable("asp_net_user_tokens");
    }
}
