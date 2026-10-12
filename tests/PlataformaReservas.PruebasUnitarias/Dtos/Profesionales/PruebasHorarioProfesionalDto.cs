using FluentAssertions;
using PlataformaReservas.Aplicacion.Dtos.Profesionales;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Dtos.Profesionales;

public sealed class PruebasHorarioProfesionalDto
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ConvertirADto_ordena_los_intervalos_de_lunes_a_domingo_y_por_hora_y_las_excepciones_por_fecha()
    {
        Profesional profesional = Profesional.Crear(Guid.CreateVersion7(), Guid.CreateVersion7(), new NombrePersona("Ana Garcia"), Ahora);
        profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Sunday, Intervalo(10, 13));
        profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Tuesday, Intervalo(9, 14));
        profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Monday, Intervalo(16, 20));
        profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Monday, Intervalo(9, 14));
        profesional.MarcarNoDisponible(Guid.CreateVersion7(), new DateOnly(2026, 12, 31), null);
        profesional.MarcarNoDisponible(Guid.CreateVersion7(), new DateOnly(2026, 12, 25), new MotivoExcepcion("Navidad"));

        HorarioProfesionalDto dto = HorarioProfesionalDto.ConvertirADto(profesional);

        dto.ProfesionalId.Should().Be(profesional.Id);
        dto.Intervalos.Select(i => (i.DiaSemana, i.HoraInicio)).Should().Equal(
            (DayOfWeek.Monday, new TimeOnly(9, 0)), (DayOfWeek.Monday, new TimeOnly(16, 0)), (DayOfWeek.Tuesday, new TimeOnly(9, 0)),
            (DayOfWeek.Sunday, new TimeOnly(10, 0)));
        dto.Excepciones.Should().Equal(
            new ExcepcionHorarioDto(new DateOnly(2026, 12, 25), "Navidad"), new ExcepcionHorarioDto(new DateOnly(2026, 12, 31), null));
    }

    private static IntervaloHorario Intervalo(int horaInicio, int horaFin)
    {
        return new IntervaloHorario(new TimeOnly(horaInicio, 0), new TimeOnly(horaFin, 0));
    }
}
