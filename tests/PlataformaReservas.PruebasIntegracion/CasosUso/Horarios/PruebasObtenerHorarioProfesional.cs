using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.CasosUso.Horarios;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Horarios;

[Collection("BaseDatos")]
public sealed class PruebasObtenerHorarioProfesional(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Devuelve_los_intervalos_de_lunes_a_domingo_y_las_excepciones_en_orden()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Sunday, Intervalo(10, 13));
        profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Tuesday, Intervalo(9, 14));
        profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Monday, Intervalo(16, 20));
        profesional.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Monday, Intervalo(9, 14));
        profesional.MarcarNoDisponible(Guid.CreateVersion7(), new DateOnly(2026, 12, 31), null);
        profesional.MarcarNoDisponible(Guid.CreateVersion7(), new DateOnly(2026, 12, 25), new MotivoExcepcion("Navidad"));
        await EscenarioEmpresa.GuardarAsync(baseDatos, profesional);

        Resultado<HorarioProfesionalDto> resultado = await ObtenerAsync(empresa.Id, profesional.Id);

        HorarioProfesionalDto horario = resultado.Valor!;
        horario.Intervalos.Select(i => (i.DiaSemana, i.HoraInicio)).Should().Equal(
            (DayOfWeek.Monday, new TimeOnly(9, 0)), (DayOfWeek.Monday, new TimeOnly(16, 0)), (DayOfWeek.Tuesday, new TimeOnly(9, 0)),
            (DayOfWeek.Sunday, new TimeOnly(10, 0)));
        horario.Excepciones.Should().Equal(
            new ExcepcionHorarioDto(new DateOnly(2026, 12, 25), "Navidad"), new ExcepcionHorarioDto(new DateOnly(2026, 12, 31), null));
    }

    [Fact]
    public async Task Un_profesional_de_otra_empresa_no_se_encuentra()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional ajeno = DominioPrueba.Profesional(ajena.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, ajeno);

        Resultado<HorarioProfesionalDto> resultado = await ObtenerAsync(propia.Id, ajeno.Id);

        resultado.Codigo.Should().Be(CodigoError.NoEncontrado);
    }

    [Fact]
    public async Task Un_profesional_sin_horario_devuelve_las_listas_vacias()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, profesional);

        Resultado<HorarioProfesionalDto> resultado = await ObtenerAsync(empresa.Id, profesional.Id);

        resultado.EsExito.Should().BeTrue();
        resultado.Valor!.Intervalos.Should().BeEmpty();
        resultado.Valor.Excepciones.Should().BeEmpty();
    }

    [Fact]
    public async Task Un_profesional_inexistente_no_se_encuentra()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);

        Resultado<HorarioProfesionalDto> resultado = await ObtenerAsync(empresa.Id, Guid.CreateVersion7());

        resultado.Codigo.Should().Be(CodigoError.NoEncontrado);
    }

    private static IntervaloHorario Intervalo(int horaInicio, int horaFin)
    {
        return new IntervaloHorario(new TimeOnly(horaInicio, 0), new TimeOnly(horaFin, 0));
    }

    private async Task<Resultado<HorarioProfesionalDto>> ObtenerAsync(Guid empresaId, Guid profesionalId)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        return await ambito.ServiceProvider.GetRequiredService<ObtenerHorarioProfesional>()
            .EjecutarAsync(new ObtenerHorarioProfesionalComando(profesionalId), CancellationToken.None);
    }
}
