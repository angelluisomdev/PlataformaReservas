using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Infraestructura.Persistencia.Conversores;

namespace PlataformaReservas.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionExcepcionHorario : IEntityTypeConfiguration<ExcepcionHorario>
{
    public void Configure(EntityTypeBuilder<ExcepcionHorario> builder)
    {
        builder.ToTable("excepciones_horario");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Motivo).HasConversion<ConversorMotivoExcepcion>().HasMaxLength(200);

        builder.HasIndex(x => new { x.ProfesionalId, x.Fecha }).IsUnique();
    }
}
