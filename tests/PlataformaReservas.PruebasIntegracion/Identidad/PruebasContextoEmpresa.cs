using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using PlataformaReservas.Infraestructura.Identidad;

namespace PlataformaReservas.PruebasIntegracion.Identidad;

public sealed class PruebasContextoEmpresa
{
    [Fact]
    public void Con_el_claim_devuelve_la_empresa()
    {
        Guid empresaId = Guid.CreateVersion7();
        ContextoEmpresa contexto = Contexto(new Claim(FabricaClaimsUsuario.ClaimEmpresa, empresaId.ToString()));

        contexto.TieneEmpresa.Should().BeTrue();
        contexto.EmpresaActualId.Should().Be(empresaId);
    }

    [Fact]
    public void Sin_el_claim_no_hay_empresa_y_pedirla_lanza()
    {
        ContextoEmpresa contexto = Contexto();

        Func<Guid> leer = () => contexto.EmpresaActualId;

        contexto.TieneEmpresa.Should().BeFalse();
        leer.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("no-es-un-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Un_claim_no_valido_no_da_empresa(string valor)
    {
        ContextoEmpresa contexto = Contexto(new Claim(FabricaClaimsUsuario.ClaimEmpresa, valor));

        Func<Guid> leer = () => contexto.EmpresaActualId;

        contexto.TieneEmpresa.Should().BeFalse();
        leer.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Con_el_estado_sin_resolver_no_hay_empresa()
    {
        ContextoEmpresa contexto = new(new ProveedorFijo(new TaskCompletionSource<AuthenticationState>().Task));

        Func<Guid> leer = () => contexto.EmpresaActualId;

        contexto.TieneEmpresa.Should().BeFalse();
        leer.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Con_el_estado_nunca_fijado_no_hay_empresa()
    {
        ContextoEmpresa contexto = new(new ProveedorSinEstado());

        Func<Guid> leer = () => contexto.EmpresaActualId;

        contexto.TieneEmpresa.Should().BeFalse();
        leer.Should().Throw<InvalidOperationException>().WithMessage("No hay empresa en el contexto*");
    }

    private static ContextoEmpresa Contexto(params Claim[] claims)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity(claims, "Prueba"));
        return new ContextoEmpresa(new ProveedorFijo(Task.FromResult(new AuthenticationState(principal))));
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
