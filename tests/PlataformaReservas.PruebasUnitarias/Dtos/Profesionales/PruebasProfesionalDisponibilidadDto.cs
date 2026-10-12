using FluentAssertions;
using PlataformaReservas.Aplicacion.Dtos.Profesionales;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Dtos.Profesionales;

public sealed class PruebasProfesionalDisponibilidadDto
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ConvertirADto_incluye_el_nombre_los_intervalos_y_las_fechas_no_disponibles()
    {
        Profesional profesional = Profesional.Crear(Guid.CreateVersion7(), Guid.CreateVersion7(), new NombrePersona("Ana Garcia"), Ahora);
        Guid horarioId = Guid.CreateVersion7();
        profesional.AgregarIntervalo(horarioId, DayOfWeek.Monday, new IntervaloHorario(new TimeOnly(9, 0), new TimeOnly(14, 0)));
        profesional.MarcarNoDisponible(Guid.CreateVersion7(), new DateOnly(2026, 12, 25), new MotivoExcepcion("Navidad"));
        profesional.MarcarNoDisponible(Guid.CreateVersion7(), new DateOnly(2026, 12, 31), null);

        ProfesionalDisponibilidadDto dto = ProfesionalDisponibilidadDto.ConvertirADto(profesional);

        dto.Id.Should().Be(profesional.Id);
        dto.NombreCompleto.Should().Be("Ana Garcia");
        dto.Horarios.Should().Equal(new IntervaloHorarioDto(horarioId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(14, 0)));
        dto.FechasNoDisponibles.Should().BeEquivalentTo([new DateOnly(2026, 12, 25), new DateOnly(2026, 12, 31)]);
    }

    [Fact]
    public void ConvertirADto_de_un_profesional_sin_horario_da_listas_vacias()
    {
        Profesional profesional = Profesional.Crear(Guid.CreateVersion7(), Guid.CreateVersion7(), new NombrePersona("Ana Garcia"), Ahora);

        ProfesionalDisponibilidadDto dto = ProfesionalDisponibilidadDto.ConvertirADto(profesional);

        dto.Horarios.Should().BeEmpty();
        dto.FechasNoDisponibles.Should().BeEmpty();
    }
}
