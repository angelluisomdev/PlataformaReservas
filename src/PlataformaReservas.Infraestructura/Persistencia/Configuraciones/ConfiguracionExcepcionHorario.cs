using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionExcepcionHorario : IEntityTypeConfiguration<ExcepcionHorario>
{
    public void Configure(EntityTypeBuilder<ExcepcionHorario> builder)
    {
        builder.ToTable("excepciones_horario");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Motivo).HasMaxLength(200);

        // Una excepcion por profesional y fecha (RN-45).
        builder.HasIndex(x => new { x.ProfesionalId, x.Fecha }).IsUnique();
    }
}
