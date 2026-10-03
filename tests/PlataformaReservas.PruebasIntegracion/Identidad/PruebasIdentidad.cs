using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Identidad;

[Collection("BaseDatos")]
public sealed class PruebasIdentidad(BaseDatosFixture baseDatos)
{
    private static readonly DateTime Ahora = new(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc);

    private const string Clave = "ClaveSegura1!";

    private static string CorreoUnico() => $"{Guid.CreateVersion7():N}@prueba.es";

    [Fact]
    public async Task Un_usuario_creado_se_encuentra_por_correo_con_sus_datos()
    {
        ServiceProvider servicios = baseDatos.ServiciosIdentidad;
        await using AsyncServiceScope ambito = servicios.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        string correo = CorreoUnico();

        IdentityResult resultado = await usuarios.CreateAsync(
            Usuario.Crear(correo, "Ana Garcia", "600111222", Ahora), "ClaveSegura1!");
        Usuario? encontrado = await usuarios.FindByEmailAsync(correo);

        resultado.Succeeded.Should().BeTrue();
        encontrado.Should().NotBeNull();
        encontrado!.NombreCompleto.Should().Be("Ana Garcia");
        encontrado.Telefono.Should().Be("600111222");
        encontrado.Activo.Should().BeTrue();
    }

    [Fact]
    public void Sin_protector_registrado_el_contexto_no_construye_el_modelo()
    {
        DbContextOptions<ContextoDatos> opciones = new DbContextOptionsBuilder<ContextoDatos>()
            .UseNpgsql(baseDatos.CadenaConexion)
            .UseSnakeCaseNamingConvention()
            .UseApplicationServiceProvider(new ServiceCollection().BuildServiceProvider())
            .EnableServiceProviderCaching(false)
            .Options;
        using ContextoDatos db = new(opciones);

        Action construir = () => _ = db.Model;

        construir.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task El_principal_lleva_empresa_id_solo_si_hay_pertenencia()
    {
        ServiceProvider servicios = baseDatos.ServiciosIdentidad;
        await using AsyncServiceScope ambito = servicios.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        IUserClaimsPrincipalFactory<Usuario> fabrica = ambito.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<Usuario>>();

        Usuario propietario = Usuario.Crear(CorreoUnico(), "Propietario", null, Ahora);
        Usuario cliente = Usuario.Crear(CorreoUnico(), "Cliente", null, Ahora);
        (await usuarios.CreateAsync(propietario, "ClaveSegura1!")).Succeeded.Should().BeTrue();
        (await usuarios.CreateAsync(cliente, "ClaveSegura1!")).Succeeded.Should().BeTrue();

        Guid empresaId = await CrearEmpresaConMiembroAsync(ambito.ServiceProvider, propietario.Id);

        ClaimsPrincipal conPertenencia = await fabrica.CreateAsync(propietario);
        ClaimsPrincipal sinPertenencia = await fabrica.CreateAsync(cliente);

        conPertenencia.FindFirstValue(FabricaClaimsUsuario.ClaimEmpresa).Should().Be(empresaId.ToString());
        sinPertenencia.FindFirst(FabricaClaimsUsuario.ClaimEmpresa).Should().BeNull();
    }

    [Fact]
    public async Task Los_roles_cliente_y_propietario_existen_por_migracion()
    {
        ServiceProvider servicios = baseDatos.ServiciosIdentidad;
        await using AsyncServiceScope ambito = servicios.CreateAsyncScope();
        RoleManager<IdentityRole<Guid>> roles = ambito.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        (await roles.RoleExistsAsync("Cliente")).Should().BeTrue();
        (await roles.RoleExistsAsync("Propietario")).Should().BeTrue();
    }

    [Fact]
    public async Task EsPropietario_exige_el_rol_y_el_claim_de_empresa()
    {
        ServiceProvider servicios = baseDatos.ServiciosIdentidad;
        IAuthorizationService autorizacion = servicios.GetRequiredService<IAuthorizationService>();

        ClaimsPrincipal sinEmpresa = Principal(new Claim(ClaimTypes.Role, "Propietario"));
        ClaimsPrincipal conEmpresa = Principal(
            new Claim(ClaimTypes.Role, "Propietario"),
            new Claim(FabricaClaimsUsuario.ClaimEmpresa, Guid.CreateVersion7().ToString()));
        ClaimsPrincipal cliente = Principal(new Claim(ClaimTypes.Role, "Cliente"));

        (await autorizacion.AuthorizeAsync(sinEmpresa, "EsPropietario")).Succeeded.Should().BeFalse();
        (await autorizacion.AuthorizeAsync(conEmpresa, "EsPropietario")).Succeeded.Should().BeTrue();
        (await autorizacion.AuthorizeAsync(cliente, "EsCliente")).Succeeded.Should().BeTrue();
        (await autorizacion.AuthorizeAsync(cliente, "EsPropietario")).Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Un_usuario_inactivo_no_puede_iniciar_sesion()
    {
        string correo = CorreoUnico();
        Usuario usuario = await CrearUsuarioAsync(correo);

        SignInResult activo = await ComprobarClaveAsync(usuario.Id, Clave);
        await DesactivarAsync(usuario.Id);
        SignInResult inactivo = await ComprobarClaveAsync(usuario.Id, Clave);

        activo.Succeeded.Should().BeTrue();
        inactivo.Succeeded.Should().BeFalse();
        inactivo.IsNotAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task Registrar_un_correo_duplicado_falla_con_el_cifrado_activo()
    {
        string correo = CorreoUnico();
        await CrearUsuarioAsync(correo);
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

        IdentityResult duplicado = await usuarios.CreateAsync(
            Usuario.Crear(correo.ToUpperInvariant(), "Otra Persona", null, Ahora), Clave);

        duplicado.Succeeded.Should().BeFalse();
        duplicado.Errors.Should().Contain(e => e.Code == "DuplicateEmail");
    }

    [Fact]
    public async Task La_solicitud_de_restablecimiento_responde_igual_exista_o_no_la_cuenta()
    {
        string existente = CorreoUnico();
        string inexistente = CorreoUnico();
        await CrearUsuarioAsync(existente);

        Func<Task> conCuenta = () => SolicitarRestablecimientoAsync(existente);
        Func<Task> sinCuenta = () => SolicitarRestablecimientoAsync(inexistente);

        await conCuenta.Should().NotThrowAsync();
        await sinCuenta.Should().NotThrowAsync();
        baseDatos.Remitente.EnlaceDe(existente).Should().NotBeNull();
        baseDatos.Remitente.EnlaceDe(inexistente).Should().BeNull();
    }

    [Fact]
    public async Task Una_cuenta_inactiva_no_genera_enlace_de_restablecimiento()
    {
        string correo = CorreoUnico();
        Usuario usuario = await CrearUsuarioAsync(correo);
        await DesactivarAsync(usuario.Id);

        await SolicitarRestablecimientoAsync(correo);

        baseDatos.Remitente.EnlaceDe(correo).Should().BeNull();
    }

    [Fact]
    public async Task Restablecer_la_contrasena_renueva_el_sello_de_seguridad()
    {
        string correo = CorreoUnico();
        Usuario creado = await CrearUsuarioAsync(correo);
        await SolicitarRestablecimientoAsync(correo);
        string token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(baseDatos.Remitente.EnlaceDe(correo)!));

        await using (AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope())
        {
            UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
            Usuario usuario = (await usuarios.FindByEmailAsync(correo))!;
            string selloAntes = (await usuarios.GetSecurityStampAsync(usuario))!;

            IdentityResult restablecido = await usuarios.ResetPasswordAsync(usuario, token, "ClaveNueva2!");

            restablecido.Succeeded.Should().BeTrue();
            (await usuarios.GetSecurityStampAsync(usuario)).Should().NotBe(selloAntes);
        }

        (await ComprobarClaveAsync(creado.Id, "ClaveNueva2!")).Succeeded.Should().BeTrue();
        (await ComprobarClaveAsync(creado.Id, Clave)).Succeeded.Should().BeFalse();
    }

    private static ClaimsPrincipal Principal(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Prueba"));
    }

    private async Task<Usuario> CrearUsuarioAsync(string correo)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        Usuario usuario = Usuario.Crear(correo, "Ana Garcia", null, Ahora);

        (await usuarios.CreateAsync(usuario, Clave)).Succeeded.Should().BeTrue();
        return usuario;
    }

    private async Task<SignInResult> ComprobarClaveAsync(Guid usuarioId, string clave)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        SignInManager<Usuario> inicioSesion = ambito.ServiceProvider.GetRequiredService<SignInManager<Usuario>>();

        Usuario usuario = (await usuarios.FindByIdAsync(usuarioId.ToString()))!;
        return await inicioSesion.CheckPasswordSignInAsync(usuario, clave, lockoutOnFailure: true);
    }

    private async Task DesactivarAsync(Guid usuarioId)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();

        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE asp_net_users SET activo = false WHERE id = {usuarioId}");
    }

    private async Task SolicitarRestablecimientoAsync(string correo)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        SolicitudRestablecimientoClave solicitud = ambito.ServiceProvider.GetRequiredService<SolicitudRestablecimientoClave>();

        await solicitud.SolicitarAsync(correo, codigo => codigo);
    }

    private static async Task<Guid> CrearEmpresaConMiembroAsync(IServiceProvider servicios, Guid usuarioId)
    {
        ContextoDatos db = servicios.GetRequiredService<ContextoDatos>();
        string sufijo = Guid.CreateVersion7().ToString("N");

        Categoria categoria = Categoria.Crear(Guid.CreateVersion7(), $"Categoria {sufijo}", Slug.Desde($"cat-{sufijo}"));
        Empresa empresa = Empresa.Crear(
            Guid.CreateVersion7(), "Empresa Prueba", Slug.Desde($"empresa-{sufijo}"), categoria.Id,
            null, "contacto@prueba.es", "600000000", "Calle Mayor 1", "28001", "Madrid", Ahora);
        MiembroEmpresa miembro = MiembroEmpresa.Crear(
            Guid.CreateVersion7(), empresa.Id, usuarioId, RolMiembro.Propietario, Ahora);

        db.AddRange(categoria, empresa, miembro);
        await db.SaveChangesAsync();
        return empresa.Id;
    }
}
