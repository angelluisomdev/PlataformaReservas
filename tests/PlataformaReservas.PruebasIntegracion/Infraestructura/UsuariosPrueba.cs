using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.PruebasIntegracion.Infraestructura;

public static class UsuariosPrueba
{
    public const string Clave = "ClaveSegura1!";

    public static readonly DateTime Ahora = new(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc);

    public static string CorreoUnico() => $"{Guid.CreateVersion7():N}@prueba.es";

    public static async Task<Usuario> CrearAsync(
        BaseDatosFixture baseDatos, string correo, string nombreCompleto = "Ana Garcia", string? telefono = null)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        Usuario usuario = Usuario.Crear(correo, new NombrePersona(nombreCompleto), telefono is null ? null : new Telefono(telefono), Ahora);

        (await usuarios.CreateAsync(usuario, Clave)).Succeeded.Should().BeTrue();
        return usuario;
    }

    public static async Task<SignInResult> ComprobarClaveAsync(BaseDatosFixture baseDatos, Guid usuarioId, string clave)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        SignInManager<Usuario> inicioSesion = ambito.ServiceProvider.GetRequiredService<SignInManager<Usuario>>();

        Usuario usuario = (await usuarios.FindByIdAsync(usuarioId.ToString()))!;
        return await inicioSesion.CheckPasswordSignInAsync(usuario, clave, lockoutOnFailure: true);
    }

    public static async Task DesactivarAsync(BaseDatosFixture baseDatos, Guid usuarioId)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();

        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE asp_net_users SET activo = false WHERE id = {usuarioId}");
    }
}
