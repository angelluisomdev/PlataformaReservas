using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.Infraestructura.Identidad;

public sealed partial class ServicioCuentas(
    UserManager<Usuario> usuarios, ContextoDatos db, IContextoEmpresa contexto, ILogger<ServicioCuentas> registro) : IServicioCuentas
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
            resultado = await usuarios.AddToRolesAsync(usuario, [RolesIdentidad.Propietario, RolesIdentidad.Cliente]);
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
            EventosSeguridad.AccesoEntreEmpresas(registro, contexto.EmpresaActualId, empresaId);
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

        RegistrarSesionesEmpresaInvalidadas(registro, empresaId);
    }

    public async Task<(NombrePersona Nombre, Telefono? Telefono)?> ObtenerContactoAsync(Guid usuarioId, CancellationToken ct)
    {
        Usuario? usuario = await usuarios.FindByIdAsync(usuarioId.ToString());
        if (usuario is null)
        {
            return null;
        }

        return (new NombrePersona(usuario.NombreCompleto), usuario.Telefono is null ? null : new Telefono(usuario.Telefono));
    }

    public async Task EstablecerTelefonoAsync(Guid usuarioId, Telefono telefono, CancellationToken ct)
    {
        Usuario usuario = await ObtenerDeLaSesionAsync(usuarioId);
        usuario.EstablecerTelefono(telefono);
        ComprobarGuardado(await usuarios.UpdateAsync(usuario), "el telefono");
    }

    public async Task ActualizarPerfilAsync(Guid usuarioId, NombrePersona nombre, Telefono? telefono, CancellationToken ct)
    {
        Usuario usuario = await ObtenerDeLaSesionAsync(usuarioId);
        usuario.ActualizarPerfil(nombre, telefono);
        ComprobarGuardado(await usuarios.UpdateAsync(usuario), "el perfil");
    }

    public async Task<bool> EstaActivaAsync(Guid usuarioId, CancellationToken ct)
    {
        return (await ObtenerDeLaSesionAsync(usuarioId)).Activo;
    }

    public async Task DarDeBajaAsync(Guid usuarioId, CancellationToken ct)
    {
        Usuario usuario = await ObtenerDeLaSesionAsync(usuarioId);
        usuario.DarDeBaja();
        ComprobarGuardado(await usuarios.UpdateSecurityStampAsync(usuario), "la baja");
        RegistrarBajaCuenta(registro, usuarioId);
    }

    private async Task<Usuario> ObtenerDeLaSesionAsync(Guid usuarioId)
    {
        return await usuarios.FindByIdAsync(usuarioId.ToString())
            ?? throw new InvalidOperationException("El usuario de la sesion no existe.");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Cuenta {UsuarioId} dada de baja")]
    private static partial void RegistrarBajaCuenta(ILogger registro, Guid usuarioId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sesiones de los miembros de la empresa {EmpresaId} invalidadas")]
    private static partial void RegistrarSesionesEmpresaInvalidadas(ILogger registro, Guid empresaId);

    private static void ComprobarGuardado(IdentityResult resultado, string que)
    {
        if (!resultado.Succeeded)
        {
            throw new InvalidOperationException(
                $"No se pudo guardar {que}: {string.Join(", ", resultado.Errors.Select(e => e.Code))}");
        }
    }
}
