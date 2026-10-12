using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos.Profesionales;
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

        return Resultado<HorarioProfesionalDto>.Exito(HorarioProfesionalDto.ConvertirADto(profesional));
    }
}
