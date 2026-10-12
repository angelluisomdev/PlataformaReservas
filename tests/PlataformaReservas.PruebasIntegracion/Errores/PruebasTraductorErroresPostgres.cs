using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PlataformaReservas.Infraestructura.Errores;

namespace PlataformaReservas.PruebasIntegracion.Errores;

public sealed class PruebasTraductorErroresPostgres
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Una_violacion_de_exclusion_es_un_hueco_ocupado(bool insertaReservas)
    {
        TraductorErroresPostgres.EsHuecoOcupado(ErrorDeGuardado("23P01"), insertaReservas).Should().BeTrue();
    }

    [Fact]
    public void Un_interbloqueo_al_insertar_reservas_es_un_hueco_ocupado()
    {
        TraductorErroresPostgres.EsHuecoOcupado(ErrorDeGuardado("40P01"), insertaReservas: true).Should().BeTrue();
    }

    [Fact]
    public void Un_interbloqueo_envuelto_como_fallo_transitorio_al_insertar_reservas_es_un_hueco_ocupado()
    {
        InvalidOperationException transitorio = new("Fallo transitorio.", ErrorDeGuardado("40P01"));

        TraductorErroresPostgres.EsHuecoOcupado(transitorio, insertaReservas: true).Should().BeTrue();
    }

    [Fact]
    public void Un_interbloqueo_sin_insertar_reservas_no_se_traduce()
    {
        TraductorErroresPostgres.EsHuecoOcupado(ErrorDeGuardado("40P01"), insertaReservas: false).Should().BeFalse();
    }

    [Theory]
    [InlineData("23505")]
    [InlineData("23503")]
    public void Otro_error_de_postgres_no_se_traduce(string codigo)
    {
        TraductorErroresPostgres.EsHuecoOcupado(ErrorDeGuardado(codigo), insertaReservas: true).Should().BeFalse();
    }

    [Fact]
    public void Un_error_sin_excepcion_de_postgres_no_se_traduce()
    {
        DbUpdateException error = new("Fallo al guardar.", new InvalidOperationException("Otro fallo."));

        TraductorErroresPostgres.EsHuecoOcupado(error, insertaReservas: true).Should().BeFalse();
    }

    private static DbUpdateException ErrorDeGuardado(string codigo)
    {
        return new DbUpdateException("Fallo al guardar.", new PostgresException("Restriccion violada.", "ERROR", "ERROR", codigo));
    }
}
