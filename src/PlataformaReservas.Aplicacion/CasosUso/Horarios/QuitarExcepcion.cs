using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;

namespace PlataformaReservas.Aplicacion.CasosUso.Horarios;

public sealed class QuitarExcepcion(IRepositorioProfesionales profesionales, IUnidadTrabajo unidadTrabajo)
{
    public async Task<Resultado> EjecutarAsync(QuitarExcepcionComando comando, CancellationToken ct)
    {
        Profesional? profesional = await profesionales.ObtenerParaModificarAsync(comando.ProfesionalId, ct);
        if (profesional is null || !profesional.Excepciones.Any(x => x.Fecha == comando.Fecha))
        {
            return Resultado.Fallo(CodigoError.NoEncontrado, "No se encuentra esa excepcion de horario.");
        }

        profesional.QuitarExcepcion(comando.Fecha);
        await unidadTrabajo.GuardarCambiosAsync(ct);
        return Resultado.Exito();
    }
}
