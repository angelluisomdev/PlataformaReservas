using System.Globalization;
using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.ValueObjects;

public sealed class PruebasObjetosValor
{
    private static readonly DateTime Base = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);

    private static FranjaHoraria Franja(int inicioMinutos, int finMinutos)
    {
        return new FranjaHoraria(Base.AddMinutes(inicioMinutos), Base.AddMinutes(finMinutos));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void FranjaHoraria_rechaza_instantes_que_no_son_utc(DateTimeKind tipo)
    {
        DateTime inicio = DateTime.SpecifyKind(Base, tipo);
        DateTime fin = DateTime.SpecifyKind(Base.AddHours(1), tipo);

        Action crear = () => _ = new FranjaHoraria(inicio, fin);

        crear.Should().Throw<DominioException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    public void FranjaHoraria_rechaza_fin_no_posterior_al_inicio(int finMinutos)
    {
        Action crear = () => _ = Franja(0, finMinutos);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void FranjaHoraria_calcula_su_duracion()
    {
        Franja(0, 45).DuracionMinutos.Should().Be(45);
    }

    [Theory]
    [InlineData(60, 120, false)]   // consecutiva por detras: [9:00,10:00) y [10:00,11:00)
    [InlineData(-60, 0, false)]    // consecutiva por delante
    [InlineData(30, 90, true)]     // solape parcial
    [InlineData(15, 45, true)]     // contenida
    [InlineData(-30, 90, true)]    // la contiene
    [InlineData(0, 60, true)]      // identica
    public void FranjaHoraria_se_solapa_solo_si_comparte_tiempo(int inicio, int fin, bool esperado)
    {
        FranjaHoraria referencia = Franja(0, 60);
        FranjaHoraria otra = Franja(inicio, fin);

        referencia.SeSolapaCon(otra).Should().Be(esperado);
        otra.SeSolapaCon(referencia).Should().Be(esperado);
    }

    [Theory]
    [InlineData("10:00", "10:00")]
    [InlineData("11:00", "10:00")]
    public void IntervaloHorario_rechaza_inicio_no_anterior_al_fin(string inicio, string fin)
    {
        Action crear = () => _ = new IntervaloHorario(TimeOnly.Parse(inicio, CultureInfo.InvariantCulture), TimeOnly.Parse(fin, CultureInfo.InvariantCulture));

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void IntervaloHorario_calcula_su_duracion()
    {
        IntervaloHorario manana = new(new TimeOnly(9, 0), new TimeOnly(14, 0));

        manana.DuracionMinutos.Should().Be(300);
    }

    [Theory]
    [InlineData("Peluquería Ana", "peluqueria-ana")]
    [InlineData("  Barbería   El  Niño!! ", "barberia-el-nino")]
    [InlineData("Taller 2000", "taller-2000")]
    public void Slug_normaliza_el_texto(string texto, string esperado)
    {
        Slug.Desde(texto).Valor.Should().Be(esperado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("¡!")]
    [InlineData("ab")]
    public void Slug_rechaza_textos_sin_caracteres_suficientes(string texto)
    {
        Action crear = () => Slug.Desde(texto);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Slug_con_sufijo_anade_el_numero()
    {
        Slug.Desde("peluqueria-ana").ConSufijo(2).Valor.Should().Be("peluqueria-ana-2");
    }

    [Fact]
    public void Slug_con_sufijo_no_supera_la_longitud_maxima()
    {
        Slug largo = Slug.Desde(new string('a', Slug.LongitudMaxima));

        largo.ConSufijo(12).Valor.Length.Should().BeLessThanOrEqualTo(Slug.LongitudMaxima);
    }
}
