using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;

namespace PlataformaReservas.Aplicacion.CasosUso.Profesionales;

public sealed class ActualizarProfesional(IRepositorioProfesionales profesionales, IUnidadTrabajo unidadTrabajo)
{
    public async Task<Resultado> EjecutarAsync(ActualizarProfesionalComando comando, CancellationToken ct)
    {
        DatosProfesional datos;
        try
        {
            datos = DatosProfesional.Desde(comando.NombreCompleto, comando.Email, comando.Telefono);
        }
        catch (DominioException error)
        {
            return Resultado.Fallo(CodigoError.Validacion, error.Message);
        }

        Profesional? profesional = await profesionales.ObtenerParaModificarAsync(comando.Id, ct);
        if (profesional is null)
        {
            return Resultado.Fallo(CodigoError.NoEncontrado, "No se encuentra el profesional.");
        }

        profesional.ActualizarDatos(datos.NombreCompleto, datos.Email, datos.Telefono);
        await unidadTrabajo.GuardarCambiosAsync(ct);
        return Resultado.Exito();
    }
}
