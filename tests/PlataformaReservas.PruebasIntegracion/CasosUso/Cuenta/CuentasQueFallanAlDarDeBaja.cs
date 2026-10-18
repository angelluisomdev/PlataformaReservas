using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Cuenta;

public sealed class CuentasQueFallanAlDarDeBaja(IServicioCuentas real) : IServicioCuentas
{
    public Task<Resultado<Guid>> CrearPropietarioAsync(
        Email email, NombrePersona nombreCompleto, Telefono? telefono, string clave, DateTime ahoraUtc, CancellationToken ct)
        => real.CrearPropietarioAsync(email, nombreCompleto, telefono, clave, ahoraUtc, ct);

    public Task RenovarSelloMiembrosAsync(Guid empresaId, CancellationToken ct) => real.RenovarSelloMiembrosAsync(empresaId, ct);

    public Task<(NombrePersona Nombre, Telefono? Telefono)?> ObtenerContactoAsync(Guid usuarioId, CancellationToken ct)
        => real.ObtenerContactoAsync(usuarioId, ct);

    public Task EstablecerTelefonoAsync(Guid usuarioId, Telefono telefono, CancellationToken ct)
        => real.EstablecerTelefonoAsync(usuarioId, telefono, ct);

    public Task ActualizarPerfilAsync(Guid usuarioId, NombrePersona nombre, Telefono? telefono, CancellationToken ct)
        => real.ActualizarPerfilAsync(usuarioId, nombre, telefono, ct);

    public Task<bool> EstaActivaAsync(Guid usuarioId, CancellationToken ct) => real.EstaActivaAsync(usuarioId, ct);

    public Task DarDeBajaAsync(Guid usuarioId, CancellationToken ct)
        => throw new InvalidOperationException("Fallo simulado al dar de baja la cuenta.");
}
