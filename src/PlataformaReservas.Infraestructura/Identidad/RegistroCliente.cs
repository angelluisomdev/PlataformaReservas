using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Eventos;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.Infraestructura.Identidad;

public sealed class RegistroCliente(UserManager<Usuario> usuarios, ContextoDatos db, IPublicadorEventos publicador)
{
    public async Task<(IdentityResult Resultado, Usuario? Usuario)> RegistrarAsync(
        string email, string nombreCompleto, string? telefono, string clave, DateTime ahoraUtc, CancellationToken ct)
    {
        Usuario usuario;
        try
        {
            usuario = Usuario.Crear(
                email,
                new NombrePersona(nombreCompleto),
                telefono is null ? null : new Telefono(telefono),
                ahoraUtc);
        }
        catch (DominioException error)
        {
            return (IdentityResult.Failed(new IdentityError { Code = "DatosNoValidos", Description = error.Message }), null);
        }

        await using IDbContextTransaction transaccion = await db.Database.BeginTransactionAsync(ct);

        IdentityResult resultado = await usuarios.CreateAsync(usuario, clave);
        if (resultado.Succeeded)
        {
            resultado = await usuarios.AddToRoleAsync(usuario, RolesIdentidad.Cliente);
        }

        if (resultado.Succeeded)
        {
            await transaccion.CommitAsync(ct);
            await publicador.PublicarAsync([new CuentaRegistrada(usuario.Id, ahoraUtc)], ct);
        }

        return (resultado, usuario);
    }
}
