using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos.Profesionales;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.Dtos.Servicios;

public sealed record ServicioReservaDto(
    Guid Id,
    string Nombre,
    int DuracionMinutos,
    decimal Precio,
    int AntelacionMaximaDias,
    IReadOnlyList<ProfesionalReservaDto> Profesionales)
{
    public static ServicioReservaDto ConvertirADto(Servicio servicio, PoliticasReserva politicas, IEnumerable<Profesional> profesionales)
    {
        return new ServicioReservaDto(
            servicio.Id,
            servicio.Nombre.Valor,
            servicio.Duracion.Minutos,
            servicio.Precio.Importe,
            politicas.AntelacionMaximaDias,
            profesionales
                .Where(p => p.Presta(servicio.Id))
                .OrderBy(p => p.NombreCompleto.Valor, OrdenEspanol.Comparador)
                .Select(ProfesionalReservaDto.ConvertirADto)
                .ToList());
    }
}
