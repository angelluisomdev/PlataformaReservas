using Microsoft.AspNetCore.Identity;

namespace PlataformaReservas.Infraestructura.Identidad;

// Vive en Infraestructura porque hereda de IdentityUser y Dominio no admite paquetes (arquitectura.md §2.2).
// Identity cifra en reposo las propiedades [ProtectedPersonalData] cuando se active ProtectPersonalData (Fase 6, D-22).
public sealed class Usuario : IdentityUser<Guid>
{
    [ProtectedPersonalData]
    public string NombreCompleto { get; private set; } = null!;

    // Opcional al registrarse, obligatorio para reservar (RN-81).
    [ProtectedPersonalData]
    public string? Telefono { get; private set; }

    public DateTime FechaAlta { get; private set; }

    // false = cuenta dada de baja: no puede iniciar sesion (RN-135).
    public bool Activo { get; private set; }
}
