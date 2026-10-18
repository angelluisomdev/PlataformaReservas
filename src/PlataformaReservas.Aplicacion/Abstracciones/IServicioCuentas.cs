using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.Abstracciones;

public interface IServicioCuentas
{
    Task<Resultado<Guid>> CrearPropietarioAsync(
        Email email, NombrePersona nombreCompleto, Telefono? telefono, string clave, DateTime ahoraUtc, CancellationToken ct);

    Task RenovarSelloMiembrosAsync(Guid empresaId, CancellationToken ct);

    Task<(NombrePersona Nombre, Telefono? Telefono)?> ObtenerContactoAsync(Guid usuarioId, CancellationToken ct);

    Task EstablecerTelefonoAsync(Guid usuarioId, Telefono telefono, CancellationToken ct);

    Task ActualizarPerfilAsync(Guid usuarioId, NombrePersona nombre, Telefono? telefono, CancellationToken ct);

    Task<bool> EstaActivaAsync(Guid usuarioId, CancellationToken ct);

    Task DarDeBajaAsync(Guid usuarioId, CancellationToken ct);
}
