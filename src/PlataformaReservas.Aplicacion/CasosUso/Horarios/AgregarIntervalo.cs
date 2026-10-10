using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.CasosUso.Horarios;

public sealed class AgregarIntervalo(IRepositorioProfesionales profesionales, IUnidadTrabajo unidadTrabajo)
{
    public async Task<Resultado<Guid>> EjecutarAsync(AgregarIntervaloComando comando, CancellationToken ct)
    {
        IntervaloHorario intervalo;
        try
        {
            intervalo = new IntervaloHorario(comando.HoraInicio, comando.HoraFin);
        }
        catch (DominioException error)
        {
            return Resultado<Guid>.Fallo(CodigoError.Validacion, error.Message);
        }

        Profesional? profesional = await profesionales.ObtenerParaModificarAsync(comando.ProfesionalId, ct);
        if (profesional is null)
        {
            return Resultado<Guid>.Fallo(CodigoError.NoEncontrado, "No se encuentra el profesional.");
        }

        Guid horarioId = Guid.CreateVersion7();
        try
        {
            profesional.AgregarIntervalo(horarioId, comando.DiaSemana, intervalo);
        }
        catch (DominioException error)
        {
            return Resultado<Guid>.Fallo(CodigoError.Validacion, error.Message);
        }

        await unidadTrabajo.GuardarCambiosAsync(ct);
        return Resultado<Guid>.Exito(horarioId);
    }
}
