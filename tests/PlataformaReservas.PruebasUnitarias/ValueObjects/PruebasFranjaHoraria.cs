using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.ValueObjects;

public sealed class PruebasFranjaHoraria
{
    private static readonly DateTime Base = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);

    private static FranjaHoraria Franja(int inicioMinutos, int finMinutos)
    {
        return new FranjaHoraria(Base.AddMinutes(inicioMinutos), Base.AddMinutes(finMinutos));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Rechaza_instantes_que_no_son_utc(DateTimeKind tipo)
    {
        DateTime inicio = DateTime.SpecifyKind(Base, tipo);
        DateTime fin = DateTime.SpecifyKind(Base.AddHours(1), tipo);

        Action crear = () => _ = new FranjaHoraria(inicio, fin);

        crear.Should().Throw<DominioException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    public void Rechaza_fin_no_posterior_al_inicio(int finMinutos)
    {
        Action crear = () => _ = Franja(0, finMinutos);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Calcula_su_duracion()
    {
        Franja(0, 45).DuracionMinutos.Should().Be(45);
    }

    [Theory]
    [InlineData(60, 120, false)]
    [InlineData(-60, 0, false)]
    [InlineData(30, 90, true)]
    [InlineData(15, 45, true)]
    [InlineData(-30, 90, true)]
    [InlineData(0, 60, true)]
    public void Se_solapa_solo_si_comparte_tiempo(int inicio, int fin, bool esperado)
    {
        FranjaHoraria referencia = Franja(0, 60);
        FranjaHoraria otra = Franja(inicio, fin);

        referencia.SeSolapaCon(otra).Should().Be(esperado);
        otra.SeSolapaCon(referencia).Should().Be(esperado);
    }

    [Theory]
    [InlineData(DateTimeKind.Utc, DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified, DateTimeKind.Utc)]
    public void Rechaza_que_solo_uno_de_los_instantes_sea_utc(DateTimeKind tipoInicio, DateTimeKind tipoFin)
    {
        DateTime inicio = DateTime.SpecifyKind(Base, tipoInicio);
        DateTime fin = DateTime.SpecifyKind(Base.AddHours(1), tipoFin);

        Action crear = () => _ = new FranjaHoraria(inicio, fin);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void La_duracion_descarta_los_segundos_sueltos()
    {
        new FranjaHoraria(Base, Base.AddSeconds(90)).DuracionMinutos.Should().Be(1);
    }
}
