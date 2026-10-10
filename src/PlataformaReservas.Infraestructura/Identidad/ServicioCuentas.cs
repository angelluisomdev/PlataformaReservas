using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.Infraestructura.Identidad;

public sealed class ServicioCuentas(UserManager<Usuario> usuarios, ContextoDatos db, IContextoEmpresa contexto) : IServicioCuentas
{
    public async Task<Resultado<Guid>> CrearPropietarioAsync(
        Email email, NombrePersona nombreCompleto, Telefono? telefono, string clave, DateTime ahoraUtc, CancellationToken ct)
    {
        Usuario usuario;
        try
        {
            usuario = Usuario.Crear(email.Valor, nombreCompleto, telefono, ahoraUtc);
        }
        catch (DominioException error)
        {
            return Resultado<Guid>.Fallo(CodigoError.Validacion, error.Message);
        }

        IdentityResult resultado = await usuarios.CreateAsync(usuario, clave);
        if (resultado.Succeeded)
        {
            resultado = await usuarios.AddToRoleAsync(usuario, RolesIdentidad.Propietario);
        }

        if (resultado.Succeeded)
        {
            return Resultado<Guid>.Exito(usuario.Id);
        }

        return resultado.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName")
            ? Resultado<Guid>.Fallo(CodigoError.Conflicto, "Ya existe una cuenta con ese correo.")
            : Resultado<Guid>.Fallo(CodigoError.Validacion, string.Join(" ", resultado.Errors.Select(e => e.Description)));
    }

    public async Task RenovarSelloMiembrosAsync(Guid empresaId, CancellationToken ct)
    {
        if (empresaId != contexto.EmpresaActualId)
        {
            throw new InvalidOperationException("No se puede renovar el sello de los miembros de otra empresa.");
        }

        List<Guid> usuarioIds = await db.MiembrosEmpresa
            .Where(m => m.EmpresaId == empresaId)
            .Select(m => m.UsuarioId)
            .ToListAsync(ct);

        foreach (Guid usuarioId in usuarioIds)
        {
            Usuario? usuario = await usuarios.FindByIdAsync(usuarioId.ToString());
            if (usuario is not null)
            {
                await usuarios.UpdateSecurityStampAsync(usuario);
            }
        }
    }
}
