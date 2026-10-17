using PlataformaReservas.Aplicacion.Dtos.Empresas;

namespace PlataformaReservas.Aplicacion.Abstracciones;

public interface IConsultasReservaPublica
{
    Task<DatosReservaDto?> ObtenerDatosReservaAsync(string slug, CancellationToken ct);
}
