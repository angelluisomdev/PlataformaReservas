using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos.Cuenta;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.CasosUso.Cuenta;

public sealed class ConsultarPerfil(IContextoUsuario contexto, IServicioCuentas cuentas)
{
    public async Task<Resultado<PerfilDto>> EjecutarAsync(CancellationToken ct)
    {
        if (!contexto.TieneUsuario)
        {
            return Resultado<PerfilDto>.Fallo(CodigoError.NoAutorizado, "Hay que iniciar sesion para ver el perfil.");
        }

        (NombrePersona Nombre, Telefono? Telefono)? contacto = await cuentas.ObtenerContactoAsync(contexto.UsuarioActualId, ct);
        return contacto is { } datos
            ? Resultado<PerfilDto>.Exito(PerfilDto.ConvertirADto(datos.Nombre, datos.Telefono))
            : Resultado<PerfilDto>.Fallo(CodigoError.NoEncontrado, "No se encuentra el perfil.");
    }
}
