using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;

namespace PlataformaReservas.Aplicacion.CasosUso.Profesionales;

public sealed class DesasociarServicio(IRepositorioProfesionales profesionales, IUnidadTrabajo unidadTrabajo)
{
    public async Task<Resultado> EjecutarAsync(DesasociarServicioComando comando, CancellationToken ct)
    {
        Profesional? profesional = await profesionales.ObtenerParaModificarAsync(comando.ProfesionalId, ct);
        if (profesional is null || !profesional.Presta(comando.ServicioId))
        {
            return Resultado.Fallo(CodigoError.NoEncontrado, "No se encuentra esa asociacion.");
        }

        profesional.DesasociarServicio(comando.ServicioId);
        await unidadTrabajo.GuardarCambiosAsync(ct);
        return Resultado.Exito();
    }
}
