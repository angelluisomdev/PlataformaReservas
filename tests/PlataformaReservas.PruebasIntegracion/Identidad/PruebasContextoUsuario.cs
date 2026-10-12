using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using PlataformaReservas.Infraestructura.Identidad;

namespace PlataformaReservas.PruebasIntegracion.Identidad;

public sealed class PruebasContextoUsuario
{
    [Fact]
    public void Con_el_claim_devuelve_el_usuario()
    {
        Guid usuarioId = Guid.CreateVersion7();
        ContextoUsuario contexto = Contexto(new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()));

        contexto.TieneUsuario.Should().BeTrue();
        contexto.UsuarioActualId.Should().Be(usuarioId);
    }

    [Fact]
    public void Sin_el_claim_no_hay_usuario_y_pedirlo_lanza()
    {
        ContextoUsuario contexto = Contexto();

        Func<Guid> leer = () => contexto.UsuarioActualId;

        contexto.TieneUsuario.Should().BeFalse();
        leer.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("no-es-un-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Un_claim_no_valido_no_da_usuario(string valor)
    {
        ContextoUsuario contexto = Contexto(new Claim(ClaimTypes.NameIdentifier, valor));

        Func<Guid> leer = () => contexto.UsuarioActualId;

        contexto.TieneUsuario.Should().BeFalse();
        leer.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Con_el_estado_sin_resolver_no_hay_usuario()
    {
        ContextoUsuario contexto = new(new ProveedorFijo(new TaskCompletionSource<AuthenticationState>().Task));

        Func<Guid> leer = () => contexto.UsuarioActualId;

        contexto.TieneUsuario.Should().BeFalse();
        leer.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Con_el_estado_nunca_fijado_no_hay_usuario()
    {
        ContextoUsuario contexto = new(new ProveedorSinEstado());

        Func<Guid> leer = () => contexto.UsuarioActualId;

        contexto.TieneUsuario.Should().BeFalse();
        leer.Should().Throw<InvalidOperationException>().WithMessage("No hay usuario en el contexto*");
    }

    private static ContextoUsuario Contexto(params Claim[] claims)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity(claims, "Prueba"));
        return new ContextoUsuario(new ProveedorFijo(Task.FromResult(new AuthenticationState(principal))));
    }

    private sealed class ProveedorFijo(Task<AuthenticationState> estado) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => estado;
    }

    private sealed class ProveedorSinEstado : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            throw new InvalidOperationException("GetAuthenticationStateAsync was called before SetAuthenticationState.");
    }
}
