using Microsoft.AspNetCore.Components.Authorization;
using PlataformaReservas.Aplicacion.Abstracciones;

namespace PlataformaReservas.Infraestructura.Identidad;

public sealed class ContextoEmpresa(AuthenticationStateProvider proveedor) : IContextoEmpresa
{
    public bool TieneEmpresa => ObtenerId() is not null;

    public Guid EmpresaActualId =>
        ObtenerId() ?? throw new InvalidOperationException(
            "No hay empresa en el contexto. Esta operacion requiere un propietario autenticado.");

    private Guid? ObtenerId() => ClaimsSesion.LeerIdentificador(proveedor, FabricaClaimsUsuario.ClaimEmpresa);
}
