using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Infraestructura.Persistencia.Configuraciones;

public sealed class ConfiguracionHorario : IEntityTypeConfiguration<Horario>
{
    public void Configure(EntityTypeBuilder<Horario> builder)
    {
        builder.ToTable("horarios");
        builder.HasKey(h => h.Id);

        builder.ComplexProperty(h => h.Intervalo, intervalo =>
        {
            intervalo.Property(i => i.HoraInicio).HasColumnName("hora_inicio");
            intervalo.Property(i => i.HoraFin).HasColumnName("hora_fin");
        });

        builder.HasIndex(h => new { h.ProfesionalId, h.DiaSemana });
    }
}
