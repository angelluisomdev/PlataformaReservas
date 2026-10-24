using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.CasosUso.Cuenta;

public sealed class EstablecerTelefono(IContextoUsuario contexto, IServicioCuentas cuentas)
{
    private const string YaTieneTelefono = "Ya tienes un telefono en tu perfil.";

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

        (NombrePersona Nombre, Telefono? Telefono)? contacto = await cuentas.ObtenerContactoAsync(contexto.UsuarioActualId, ct);
        if (contacto?.Telefono is not null)
        {
            return Resultado.Fallo(CodigoError.Conflicto, YaTieneTelefono);
        }

        try
        {
            await cuentas.EstablecerTelefonoAsync(contexto.UsuarioActualId, telefono, ct);
        }
        catch (DominioException)
        {
            // Carrera entre dos pestanas: la precondicion se cumplia al leer y otra peticion guardo antes.
            return Resultado.Fallo(CodigoError.Conflicto, YaTieneTelefono);
        }

        return Resultado.Exito();
    }
}
