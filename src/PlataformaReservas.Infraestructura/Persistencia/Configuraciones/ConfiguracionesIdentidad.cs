using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PlataformaReservas.Infraestructura.Persistencia.Configuraciones;

// IdentityDbContext fija los nombres "AspNet*" de sus tablas y la convencion snake_case no los reescribe.
// Estas configuraciones solo cambian el nombre de tabla, para que coincida con la convencion (D-13):
// asp_net_roles, asp_net_user_roles, etc. Nada mas de Identity se toca.

public sealed class ConfiguracionRol : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder)
    {
        builder.ToTable("asp_net_roles");
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
