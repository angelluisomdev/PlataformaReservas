using System.Globalization;
using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.ValueObjects;

public sealed class PruebasIntervaloHorario
{
    [Theory]
    [InlineData("10:00", "10:00")]
    [InlineData("11:00", "10:00")]
    public void Rechaza_inicio_no_anterior_al_fin(string inicio, string fin)
    {
        Action crear = () => _ = new IntervaloHorario(TimeOnly.Parse(inicio, CultureInfo.InvariantCulture), TimeOnly.Parse(fin, CultureInfo.InvariantCulture));

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Admite_un_minuto_y_el_dia_completo()
    {
        new IntervaloHorario(new TimeOnly(0, 0), new TimeOnly(0, 1)).DuracionMinutos.Should().Be(1);
        new IntervaloHorario(new TimeOnly(0, 0), new TimeOnly(23, 59)).DuracionMinutos.Should().Be(1439);
    }

    [Fact]
    public void Calcula_su_duracion()
    {
        IntervaloHorario manana = new(new TimeOnly(9, 0), new TimeOnly(14, 0));

        manana.DuracionMinutos.Should().Be(300);
    }

    [Theory]
    [InlineData("09:00", "14:00", "13:00", "16:00")]
    [InlineData("09:00", "14:00", "10:00", "11:00")]
    [InlineData("09:00", "14:00", "09:00", "14:00")]
    [InlineData("09:00", "14:00", "08:00", "09:30")]
    public void Se_solapan_si_comparten_algun_minuto(string inicioA, string finA, string inicioB, string finB)
    {
        IntervaloHorario a = Intervalo(inicioA, finA);
        IntervaloHorario b = Intervalo(inicioB, finB);

        a.SeSolapaCon(b).Should().BeTrue();
        b.SeSolapaCon(a).Should().BeTrue();
    }

    [Theory]
    [InlineData("09:00", "14:00", "14:00", "18:00")]
    [InlineData("09:00", "14:00", "16:00", "20:00")]
    public void No_se_solapan_si_son_contiguos_o_disjuntos(string inicioA, string finA, string inicioB, string finB)
    {
        IntervaloHorario a = Intervalo(inicioA, finA);
        IntervaloHorario b = Intervalo(inicioB, finB);

        a.SeSolapaCon(b).Should().BeFalse();
        b.SeSolapaCon(a).Should().BeFalse();
    }

    [Fact]
    public void Dos_intervalos_con_las_mismas_horas_son_iguales()
    {
        new IntervaloHorario(new TimeOnly(9, 0), new TimeOnly(14, 0))
            .Should().Be(new IntervaloHorario(new TimeOnly(9, 0), new TimeOnly(14, 0)));
    }

    private static IntervaloHorario Intervalo(string inicio, string fin)
    {
        return new IntervaloHorario(TimeOnly.Parse(inicio, CultureInfo.InvariantCulture), TimeOnly.Parse(fin, CultureInfo.InvariantCulture));
    }
}
