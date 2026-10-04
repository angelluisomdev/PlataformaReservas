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
    public void Calcula_su_duracion()
    {
        IntervaloHorario manana = new(new TimeOnly(9, 0), new TimeOnly(14, 0));

        manana.DuracionMinutos.Should().Be(300);
    }

    [Fact]
    public void Dos_intervalos_con_las_mismas_horas_son_iguales()
    {
        new IntervaloHorario(new TimeOnly(9, 0), new TimeOnly(14, 0))
            .Should().Be(new IntervaloHorario(new TimeOnly(9, 0), new TimeOnly(14, 0)));
    }
}
