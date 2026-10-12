using PlataformaReservas.Dominio.Enumeraciones;

namespace PlataformaReservas.Dominio.Eventos;

public sealed record ReservaCancelada(Guid ReservaId, OrigenCancelacion Origen, DateTime OcurridoEnUtc) : IEventoDominio;
