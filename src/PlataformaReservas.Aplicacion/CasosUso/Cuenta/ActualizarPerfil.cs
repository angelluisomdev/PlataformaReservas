using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.CasosUso.Cuenta;

public sealed class ActualizarPerfil(IContextoUsuario contexto, IServicioCuentas cuentas)
{
    public async Task<Resultado> EjecutarAsync(ActualizarPerfilComando comando, CancellationToken ct)
    {
        if (!contexto.TieneUsuario)
        {
            return Resultado.Fallo(CodigoError.NoAutorizado, "Hay que iniciar sesion para editar el perfil.");
        }

        NombrePersona nombre;
        Telefono? telefono;
        try
        {
            nombre = new NombrePersona(comando.NombreCompleto ?? "");
            telefono = string.IsNullOrWhiteSpace(comando.Telefono) ? null : new Telefono(comando.Telefono);
        }
        catch (DominioException error)
        {
            return Resultado.Fallo(CodigoError.Validacion, error.Message);
        }

        await cuentas.ActualizarPerfilAsync(contexto.UsuarioActualId, nombre, telefono, ct);
        return Resultado.Exito();
    }
}
