using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using PlataformaReservas.Infraestructura.Identidad;

namespace PlataformaReservas.Web.Cuenta;

internal sealed class DestinoInicioSesion(IAuthorizationService autorizacion)
{
    public const string AreaEmpresa = "app";
    public const string AreaCliente = "my-account/bookings";
    public const string Portada = "";

    public async Task<string> CalcularAsync(ClaimsPrincipal usuario)
    {
        if ((await autorizacion.AuthorizeAsync(usuario, RolesIdentidad.PoliticaPropietario)).Succeeded)
        {
            return AreaEmpresa;
        }

        if ((await autorizacion.AuthorizeAsync(usuario, RolesIdentidad.PoliticaCliente)).Succeeded)
        {
            return AreaCliente;
        }

        return Portada;
    }
}
