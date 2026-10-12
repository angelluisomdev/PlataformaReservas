using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using PlataformaReservas.Aplicacion.Abstracciones;

namespace PlataformaReservas.Infraestructura.Identidad;

public sealed class ContextoUsuario(AuthenticationStateProvider proveedor) : IContextoUsuario
{
    public bool TieneUsuario => ObtenerId() is not null;

    public Guid UsuarioActualId =>
        ObtenerId() ?? throw new InvalidOperationException(
            "No hay usuario en el contexto. Esta operacion requiere un usuario autenticado.");

    private Guid? ObtenerId() => ClaimsSesion.LeerIdentificador(proveedor, ClaimTypes.NameIdentifier);
}
