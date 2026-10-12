using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Dtos.Empresas;
using PlataformaReservas.Aplicacion.Dtos.Profesionales;
using PlataformaReservas.Aplicacion.Dtos.Reservas;
using PlataformaReservas.Aplicacion.Dtos.Servicios;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Infraestructura;

public sealed class ConsultasDisponibilidadEnMemoria : IConsultasDisponibilidad
{
    private readonly List<Empresa> _empresas = [];
    private readonly List<Servicio> _servicios = [];
    private readonly List<Profesional> _profesionales = [];
    private readonly List<Reserva> _reservas = [];

    public ConsultasDisponibilidadEnMemoria Con(params Empresa[] empresas)
    {
        _empresas.AddRange(empresas);
        return this;
    }

    public ConsultasDisponibilidadEnMemoria Con(params Servicio[] servicios)
    {
        _servicios.AddRange(servicios);
        return this;
    }

    public ConsultasDisponibilidadEnMemoria Con(params Profesional[] profesionales)
    {
        _profesionales.AddRange(profesionales);
        return this;
    }

    public ConsultasDisponibilidadEnMemoria Con(params Reserva[] reservas)
    {
        _reservas.AddRange(reservas);
        return this;
    }

    public Task<EmpresaDisponibilidadDto?> ObtenerEmpresaAsync(Guid empresaId, CancellationToken ct)
    {
        Empresa? empresa = _empresas.Find(e => e.Id == empresaId);
        return Task.FromResult(empresa is null ? null : EmpresaDisponibilidadDto.ConvertirADto(empresa));
    }

    public Task<ServicioDisponibilidadDto?> ObtenerServicioAsync(Guid empresaId, Guid servicioId, CancellationToken ct)
    {
        Servicio? servicio = _servicios.Find(s => s.EmpresaId == empresaId && s.Id == servicioId);
        return Task.FromResult(servicio is null ? null : ServicioDisponibilidadDto.ConvertirADto(servicio));
    }

    public Task<IReadOnlyList<ProfesionalDisponibilidadDto>> ObtenerProfesionalesQuePrestanAsync(
        Guid empresaId, Guid servicioId, Guid? profesionalId, CancellationToken ct)
    {
        IReadOnlyList<ProfesionalDisponibilidadDto> profesionales = _profesionales
            .Where(p => p.EmpresaId == empresaId && p.Activo && p.Presta(servicioId))
            .Where(p => profesionalId is null || p.Id == profesionalId)
            .Select(ProfesionalDisponibilidadDto.ConvertirADto)
            .ToList();
        return Task.FromResult(profesionales);
    }

    public Task<IReadOnlyList<ReservaOcupadaDto>> ObtenerReservasActivasAsync(
        Guid empresaId, IReadOnlyCollection<Guid> profesionalIds, DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct)
    {
        FranjaHoraria rango = new(desdeUtc, hastaUtc);
        IReadOnlyList<ReservaOcupadaDto> reservas = _reservas
            .Where(r => r.EmpresaId == empresaId && r.Estado != EstadoReserva.Cancelada)
            .Where(r => profesionalIds.Contains(r.ProfesionalId) && r.Franja.SeSolapaCon(rango))
            .Select(ReservaOcupadaDto.ConvertirADto)
            .ToList();
        return Task.FromResult(reservas);
    }
}
