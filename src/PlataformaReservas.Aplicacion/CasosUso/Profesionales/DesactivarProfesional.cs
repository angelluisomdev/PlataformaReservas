using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;

namespace PlataformaReservas.Aplicacion.CasosUso.Profesionales;

public sealed class DesactivarProfesional(IRepositorioProfesionales profesionales, IUnidadTrabajo unidadTrabajo)
{
    public async Task<Resultado> EjecutarAsync(DesactivarProfesionalComando comando, CancellationToken ct)
    {
        Profesional? profesional = await profesionales.ObtenerParaModificarAsync(comando.Id, ct);
        if (profesional is null)
        {
            return Resultado.Fallo(CodigoError.NoEncontrado, "No se encuentra el profesional.");
        }

        if (!profesional.Activo)
        {
            return Resultado.Fallo(CodigoError.EstadoNoValido, "El profesional ya esta desactivado.");
        }

        profesional.Desactivar();
        await unidadTrabajo.GuardarCambiosAsync(ct);
        return Resultado.Exito();
    }
}
