using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;

namespace PlataformaReservas.Aplicacion.CasosUso.Horarios;

public sealed class EliminarIntervalo(IRepositorioProfesionales profesionales, IUnidadTrabajo unidadTrabajo)
{
    public async Task<Resultado> EjecutarAsync(EliminarIntervaloComando comando, CancellationToken ct)
    {
        Profesional? profesional = await profesionales.ObtenerParaModificarAsync(comando.ProfesionalId, ct);
        if (profesional is null || !profesional.Horarios.Any(h => h.Id == comando.HorarioId))
        {
            return Resultado.Fallo(CodigoError.NoEncontrado, "No se encuentra ese intervalo de horario.");
        }

        profesional.EliminarIntervalo(comando.HorarioId);
        await unidadTrabajo.GuardarCambiosAsync(ct);
        return Resultado.Exito();
    }
}
