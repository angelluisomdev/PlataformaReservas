using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;

namespace PlataformaReservas.Aplicacion.CasosUso.Horarios;

public sealed class ObtenerHorarioProfesional(IRepositorioProfesionales profesionales)
{
    public async Task<Resultado<HorarioProfesionalDto>> EjecutarAsync(ObtenerHorarioProfesionalComando comando, CancellationToken ct)
    {
        Profesional? profesional = await profesionales.ObtenerConHorarioAsync(comando.ProfesionalId, ct);
        if (profesional is null)
        {
            return Resultado<HorarioProfesionalDto>.Fallo(CodigoError.NoEncontrado, "No se encuentra el profesional.");
        }

        List<IntervaloHorarioDto> intervalos = profesional.Horarios
            .OrderBy(h => ((int)h.DiaSemana + 6) % 7)
            .ThenBy(h => h.Intervalo.HoraInicio)
            .Select(h => new IntervaloHorarioDto(h.Id, h.DiaSemana, h.Intervalo.HoraInicio, h.Intervalo.HoraFin))
            .ToList();
        List<ExcepcionHorarioDto> excepciones = profesional.Excepciones
            .OrderBy(x => x.Fecha)
            .Select(x => new ExcepcionHorarioDto(x.Fecha, x.Motivo?.Valor))
            .ToList();

        return Resultado<HorarioProfesionalDto>.Exito(new HorarioProfesionalDto(profesional.Id, intervalos, excepciones));
    }
}
