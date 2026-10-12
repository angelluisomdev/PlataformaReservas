using PlataformaReservas.Aplicacion.Dtos.Empresas;
using PlataformaReservas.Aplicacion.Dtos.Profesionales;
using PlataformaReservas.Aplicacion.Dtos.Reservas;
using PlataformaReservas.Aplicacion.Dtos.Servicios;

namespace PlataformaReservas.Aplicacion.Abstracciones;

public interface IConsultasDisponibilidad
{
    Task<EmpresaDisponibilidadDto?> ObtenerEmpresaAsync(Guid empresaId, CancellationToken ct);

    Task<ServicioDisponibilidadDto?> ObtenerServicioAsync(Guid empresaId, Guid servicioId, CancellationToken ct);

    Task<IReadOnlyList<ProfesionalDisponibilidadDto>> ObtenerProfesionalesQuePrestanAsync(
        Guid empresaId, Guid servicioId, Guid? profesionalId, CancellationToken ct);

    Task<IReadOnlyList<ReservaOcupadaDto>> ObtenerReservasActivasAsync(
        Guid empresaId, IReadOnlyCollection<Guid> profesionalIds, DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct);
}
