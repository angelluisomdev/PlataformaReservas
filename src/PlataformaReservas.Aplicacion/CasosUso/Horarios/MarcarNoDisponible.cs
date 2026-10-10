using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.CasosUso.Horarios;

public sealed class MarcarNoDisponible(IRepositorioProfesionales profesionales, IUnidadTrabajo unidadTrabajo)
{
    public async Task<Resultado> EjecutarAsync(MarcarNoDisponibleComando comando, CancellationToken ct)
    {
        MotivoExcepcion? motivo;
        try
        {
            motivo = string.IsNullOrWhiteSpace(comando.Motivo) ? null : new MotivoExcepcion(comando.Motivo);
        }
        catch (DominioException error)
        {
            return Resultado.Fallo(CodigoError.Validacion, error.Message);
        }

        Profesional? profesional = await profesionales.ObtenerParaModificarAsync(comando.ProfesionalId, ct);
        if (profesional is null)
        {
            return Resultado.Fallo(CodigoError.NoEncontrado, "No se encuentra el profesional.");
        }

        try
        {
            profesional.MarcarNoDisponible(Guid.CreateVersion7(), comando.Fecha, motivo);
        }
        catch (DominioException error)
        {
            return Resultado.Fallo(CodigoError.Validacion, error.Message);
        }

        await unidadTrabajo.GuardarCambiosAsync(ct);
        return Resultado.Exito();
    }
}
