using System.Globalization;
using Microsoft.AspNetCore.Components.Authorization;

namespace PlataformaReservas.Infraestructura.Identidad;

internal static class ClaimsSesion
{
    public static Guid? LeerIdentificador(AuthenticationStateProvider proveedor, string tipoClaim)
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

        string? valor = estado.Result.User.FindFirst(tipoClaim)?.Value;
        return Guid.TryParse(valor, CultureInfo.InvariantCulture, out Guid id) && id != Guid.Empty ? id : null;
    }
}
