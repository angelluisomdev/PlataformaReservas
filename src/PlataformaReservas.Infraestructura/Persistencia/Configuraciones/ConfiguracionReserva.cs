using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Infraestructura.Identidad;

namespace PlataformaReservas.Infraestructura.Persistencia.Configuraciones;

// Sin constructor sin parametros: ApplyConfigurationsFromAssembly la salta y ContextoDatos la aplica con el protector.
public sealed class ConfiguracionReserva(IPersonalDataProtector? protector) : IEntityTypeConfiguration<Reserva>
{
    public void Configure(EntityTypeBuilder<Reserva> builder)
    {
        builder.ToTable("reservas", tabla =>
            tabla.HasCheckConstraint("ck_reservas_franja", "fin_utc > inicio_utc"));
        builder.HasKey(r => r.Id);

        // Nombres fijos: la restriccion EXCLUDE de la migracion los usa literalmente.
        builder.ComplexProperty(r => r.Franja, franja =>
        {
            franja.Property(f => f.InicioUtc).HasColumnName("inicio_utc");
            franja.Property(f => f.FinUtc).HasColumnName("fin_utc");
        });

        builder.Property(r => r.PrecioAplicado).HasPrecision(10, 2);
        builder.Property(r => r.Observaciones).HasMaxLength(500);

        PropertyBuilder<string> nombre = builder.Property(r => r.NombreCliente).IsRequired();
        PropertyBuilder<string> telefono = builder.Property(r => r.TelefonoCliente).IsRequired();
        if (protector is not null)
        {
            nombre.HasConversion(new ConversorCifrado(protector));
            telefono.HasConversion(new ConversorCifrado(protector));
        }

        builder.Property(r => r.Estado).HasConversion<int>();

        builder.HasOne<Empresa>().WithMany().HasForeignKey(r => r.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(r => r.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Profesional>().WithMany().HasForeignKey(r => r.ProfesionalId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Servicio>().WithMany().HasForeignKey(r => r.ServicioId).OnDelete(DeleteBehavior.Restrict);

        // Los indices sobre inicio_utc van a mano en la migracion: EF no indexa columnas de un tipo complejo.
    }
}
