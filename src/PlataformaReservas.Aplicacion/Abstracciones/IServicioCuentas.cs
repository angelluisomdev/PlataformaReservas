using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.Abstracciones;

public interface IServicioCuentas
{
    Task<Resultado<Guid>> CrearPropietarioAsync(
        Email email, NombrePersona nombreCompleto, Telefono? telefono, string clave, DateTime ahoraUtc, CancellationToken ct);

    Task RenovarSelloMiembrosAsync(Guid empresaId, CancellationToken ct);
}
