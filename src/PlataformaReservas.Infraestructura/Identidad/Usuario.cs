using Microsoft.AspNetCore.Identity;
using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Infraestructura.Identidad;

public sealed class Usuario : IdentityUser<Guid>
{
    private const int LongitudMaximaNombre = 120;
    private const int LongitudMaximaTelefono = 20;

    private Usuario()
    {
    }

    [ProtectedPersonalData]
    public string NombreCompleto { get; private set; } = null!;

    [ProtectedPersonalData]
    public string? Telefono { get; private set; }

    public DateTime FechaAlta { get; private set; }

    public bool Activo { get; private set; }

    public static Usuario Crear(string email, string nombreCompleto, string? telefono, DateTime ahoraUtc)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DominioException("El correo electronico es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(nombreCompleto))
        {
            throw new DominioException("El nombre completo es obligatorio.");
        }

        if (nombreCompleto.Trim().Length > LongitudMaximaNombre)
        {
            throw new DominioException($"El nombre completo no puede superar {LongitudMaximaNombre} caracteres.");
        }

        if (telefono is not null && (string.IsNullOrWhiteSpace(telefono) || telefono.Trim().Length > LongitudMaximaTelefono))
        {
            throw new DominioException($"El telefono no puede estar vacio ni superar {LongitudMaximaTelefono} caracteres.");
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
            NombreCompleto = nombreCompleto.Trim(),
            Telefono = telefono?.Trim(),
            FechaAlta = ahoraUtc,
            Activo = true,
        };
    }
}
