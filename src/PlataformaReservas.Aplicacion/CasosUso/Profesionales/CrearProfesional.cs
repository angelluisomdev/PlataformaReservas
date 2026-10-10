using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;

namespace PlataformaReservas.Aplicacion.CasosUso.Profesionales;

public sealed class CrearProfesional(
    IRepositorioProfesionales profesionales,
    IContextoEmpresa contexto,
    IUnidadTrabajo unidadTrabajo,
    IRelojSistema reloj)
{
    public async Task<Resultado<Guid>> EjecutarAsync(CrearProfesionalComando comando, CancellationToken ct)
    {
        DatosProfesional datos;
        try
        {
            datos = DatosProfesional.Desde(comando.NombreCompleto, comando.Email, comando.Telefono);
        }
        catch (DominioException error)
        {
            return Resultado<Guid>.Fallo(CodigoError.Validacion, error.Message);
        }

        Profesional profesional = Profesional.Crear(
            Guid.CreateVersion7(), contexto.EmpresaActualId, datos.NombreCompleto, reloj.AhoraUtc);
        profesional.ActualizarDatos(datos.NombreCompleto, datos.Email, datos.Telefono);
        profesionales.Agregar(profesional);

        await unidadTrabajo.GuardarCambiosAsync(ct);
        return Resultado<Guid>.Exito(profesional.Id);
    }
}
