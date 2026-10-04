using Microsoft.AspNetCore.Identity;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;
using TelefonoValidado = PlataformaReservas.Dominio.ValueObjects.Telefono;

namespace PlataformaReservas.Infraestructura.Identidad;

public sealed class Usuario : IdentityUser<Guid>
{
    private Usuario()
    {
    }

    [ProtectedPersonalData]
    public string NombreCompleto { get; private set; } = null!;

    [ProtectedPersonalData]
    public string? Telefono { get; private set; }

    public DateTime FechaAlta { get; private set; }

    public bool Activo { get; private set; }

    public static Usuario Crear(string email, NombrePersona nombreCompleto, TelefonoValidado? telefono, DateTime ahoraUtc)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DominioException("El correo electronico es obligatorio.");
        }

        if (ahoraUtc.Kind != DateTimeKind.Utc)
        {
            throw new DominioException("La fecha de alta del usuario debe estar en UTC.");
        }

        string correo = email.Trim();

        return new Usuario
        {
            Id = Guid.CreateVersion7(),
            UserName = correo,
            Email = correo,
            NombreCompleto = nombreCompleto.Valor,
            Telefono = telefono?.Valor,
            FechaAlta = ahoraUtc,
            Activo = true,
        };
    }
}
