using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.Infraestructura.Identidad;

public sealed class RegistroCliente(UserManager<Usuario> usuarios, ContextoDatos db)
{
    public async Task<(IdentityResult Resultado, Usuario? Usuario)> RegistrarAsync(
        string email, string nombreCompleto, string? telefono, string clave, DateTime ahoraUtc)
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

        await using IDbContextTransaction transaccion = await db.Database.BeginTransactionAsync();

        IdentityResult resultado = await usuarios.CreateAsync(usuario, clave);
        if (resultado.Succeeded)
        {
            resultado = await usuarios.AddToRoleAsync(usuario, RolesIdentidad.Cliente);
        }

        if (resultado.Succeeded)
        {
            await transaccion.CommitAsync();
        }

        return (resultado, usuario);
    }
}
