using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.CasosUso.Cuenta;

public sealed class EstablecerTelefono(IContextoUsuario contexto, IServicioCuentas cuentas)
{
    public async Task<Resultado> EjecutarAsync(EstablecerTelefonoComando comando, CancellationToken ct)
    {
        if (!contexto.TieneUsuario)
        {
            return Resultado.Fallo(CodigoError.NoAutorizado, "Hay que iniciar sesion para guardar el telefono.");
        }

        Telefono telefono;
        
        try
        {
            telefono = new Telefono(comando.Telefono ?? "");
        }
        catch (DominioException error)
        {
            return Resultado.Fallo(CodigoError.Validacion, error.Message);
        }

        if (telefono is null)
        {
            return Resultado.Fallo(CodigoError.Conflicto, "No se ha proporcionado un número de teléfono válido.");
        }

        try
        {
            await cuentas.EstablecerTelefonoAsync(contexto.UsuarioActualId, telefono, ct);
        }
        catch (DominioException error)
        {
            return Resultado.Fallo(CodigoError.Conflicto, error.Message);
        }

        return Resultado.Exito();
    }
}
