using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Infraestructura.Identidad;

namespace PlataformaReservas.Infraestructura.Persistencia.Configuraciones;

// Sin constructor sin parametros a proposito: ApplyConfigurationsFromAssembly la salta y
// ContextoDatos la aplica aparte, pasandole el protector.
public sealed class ConfiguracionReserva(IPersonalDataProtector? protector) : IEntityTypeConfiguration<Reserva>
{
    public void Configure(EntityTypeBuilder<Reserva> builder)
    {
        builder.ToTable("reservas", tabla =>
            tabla.HasCheckConstraint("ck_reservas_franja", "fin_utc > inicio_utc"));
        builder.HasKey(r => r.Id);

        // Columnas con nombre explicito: la restriccion EXCLUDE de la migracion las usa tal cual (SPEC §13).
        // La convencion generaria franja_inicio_utc y el SQL fallaria.
        builder.ComplexProperty(r => r.Franja, franja =>
        {
            franja.Property(f => f.InicioUtc).HasColumnName("inicio_utc");
            franja.Property(f => f.FinUtc).HasColumnName("fin_utc");
        });

        builder.Property(r => r.PrecioAplicado).HasPrecision(10, 2);
        builder.Property(r => r.Observaciones).HasMaxLength(500);

        // Las columnas cifradas no llevan longitud maxima: el texto cifrado ocupa mas que el original.
        // Las longitudes de negocio (120 y 20) las garantiza Reserva.Crear.
        PropertyBuilder<string> nombre = builder.Property(r => r.NombreCliente).IsRequired();
        PropertyBuilder<string> telefono = builder.Property(r => r.TelefonoCliente).IsRequired();
        if (protector is not null)
        {
            nombre.HasConversion(new ConversorCifrado(protector));
            telefono.HasConversion(new ConversorCifrado(protector));
        }

        // Estado como entero: el EXCLUDE filtra por estado <> 1 (Cancelada).
        builder.Property(r => r.Estado).HasConversion<int>();

        // DeleteBehavior.Restrict en todas las claves ajenas de reservas (SPEC §14).
        builder.HasOne<Empresa>().WithMany().HasForeignKey(r => r.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(r => r.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Profesional>().WithMany().HasForeignKey(r => r.ProfesionalId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Servicio>().WithMany().HasForeignKey(r => r.ServicioId).OnDelete(DeleteBehavior.Restrict);

        // Los indices (empresa_id, inicio_utc) y (usuario_id, inicio_utc) de SPEC §14 no se declaran aqui:
        // EF Core no indexa columnas de un tipo complejo. Van a mano en la migracion, junto al EXCLUDE.
        // No se crea B-tree (profesional_id, inicio_utc): lo cubre el indice GiST del EXCLUDE (SPEC §13).
    }
}
