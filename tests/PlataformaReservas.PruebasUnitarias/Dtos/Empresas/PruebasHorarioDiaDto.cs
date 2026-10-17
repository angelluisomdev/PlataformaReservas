using FluentAssertions;
using PlataformaReservas.Aplicacion.Dtos.Empresas;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Dtos.Empresas;

public sealed class PruebasHorarioDiaDto
{
    private static readonly DateTime Ahora = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Devuelve_los_siete_dias_de_lunes_a_domingo_y_sin_horario_queda_cerrado()
    {
        IReadOnlyList<HorarioDiaDto> semana = HorarioDiaDto.ConvertirADto([]);

        semana.Select(d => d.Dia).Should().Equal(
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
            DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday);
        semana.Should().OnlyContain(d => d.Cerrado);
    }

    [Fact]
    public void Funde_los_intervalos_que_se_solapan_de_distintos_profesionales()
    {
        IReadOnlyList<HorarioDiaDto> semana = HorarioDiaDto.ConvertirADto(Horarios(
            (DayOfWeek.Monday, 9, 14), (DayOfWeek.Monday, 13, 18)));

        Lunes(semana).Should().Equal(new IntervaloHorario(Hora(9), Hora(18)));
    }

    [Fact]
    public void Funde_los_intervalos_contiguos()
    {
        IReadOnlyList<HorarioDiaDto> semana = HorarioDiaDto.ConvertirADto(Horarios(
            (DayOfWeek.Monday, 14, 18), (DayOfWeek.Monday, 9, 14)));

        Lunes(semana).Should().Equal(new IntervaloHorario(Hora(9), Hora(18)));
    }

    [Fact]
    public void Un_intervalo_contenido_en_otro_no_lo_acorta()
    {
        IReadOnlyList<HorarioDiaDto> semana = HorarioDiaDto.ConvertirADto(Horarios(
            (DayOfWeek.Monday, 9, 20), (DayOfWeek.Monday, 10, 12)));

        Lunes(semana).Should().Equal(new IntervaloHorario(Hora(9), Hora(20)));
    }

    [Fact]
    public void Mantiene_separados_los_intervalos_con_hueco_entre_ellos_y_ordenados()
    {
        IReadOnlyList<HorarioDiaDto> semana = HorarioDiaDto.ConvertirADto(Horarios(
            (DayOfWeek.Monday, 16, 20), (DayOfWeek.Monday, 9, 14)));

        Lunes(semana).Should().Equal(new IntervaloHorario(Hora(9), Hora(14)), new IntervaloHorario(Hora(16), Hora(20)));
        semana.Where(d => d.Dia != DayOfWeek.Monday).Should().OnlyContain(d => d.Cerrado);
    }

    private static IReadOnlyList<IntervaloHorario> Lunes(IReadOnlyList<HorarioDiaDto> semana)
    {
        return semana.Single(d => d.Dia == DayOfWeek.Monday).Intervalos;
    }

    private static List<Horario> Horarios(params (DayOfWeek Dia, int Inicio, int Fin)[] tramos)
    {
        List<Horario> horarios = [];
        foreach ((DayOfWeek dia, int inicio, int fin) in tramos)
        {
            Profesional profesional = Profesional.Crear(Guid.CreateVersion7(), Guid.CreateVersion7(), new NombrePersona("Ana Garcia"), Ahora);
            profesional.AgregarIntervalo(Guid.CreateVersion7(), dia, new IntervaloHorario(Hora(inicio), Hora(fin)));
            horarios.AddRange(profesional.Horarios);
        }

        return horarios;
    }

    private static TimeOnly Hora(int hora) => new(hora, 0);
}
