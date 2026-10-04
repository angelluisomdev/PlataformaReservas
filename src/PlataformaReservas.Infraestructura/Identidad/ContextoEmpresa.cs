using System.Globalization;
using Microsoft.AspNetCore.Components.Authorization;
using PlataformaReservas.Aplicacion.Abstracciones;

namespace PlataformaReservas.Infraestructura.Identidad;

public sealed class ContextoEmpresa(AuthenticationStateProvider proveedor) : IContextoEmpresa
{
    public bool TieneEmpresa => ObtenerId() is not null;

    public Guid EmpresaActualId =>
        ObtenerId() ?? throw new InvalidOperationException(
            "No hay empresa en el contexto. Esta operacion requiere un propietario autenticado.");

    private Guid? ObtenerId()
    {
        Task<AuthenticationState> estado;
        try
        {
            estado = proveedor.GetAuthenticationStateAsync();
        }
        catch (InvalidOperationException)
        {
            return null;
        }

        if (!estado.IsCompletedSuccessfully)
        {
            return null;
        }

        string? valor = estado.Result.User.FindFirst(FabricaClaimsUsuario.ClaimEmpresa)?.Value;
        return Guid.TryParse(valor, CultureInfo.InvariantCulture, out Guid id) && id != Guid.Empty ? id : null;
    }
}
