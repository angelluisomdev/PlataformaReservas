using Microsoft.AspNetCore.Identity;

namespace PlataformaReservas.Infraestructura.Identidad;

public sealed class Usuario : IdentityUser<Guid>
{
    [ProtectedPersonalData]
    public string NombreCompleto { get; private set; } = null!;

    [ProtectedPersonalData]
    public string? Telefono { get; private set; }

    public DateTime FechaAlta { get; private set; }

    public bool Activo { get; private set; }
}
