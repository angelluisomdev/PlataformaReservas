namespace PlataformaReservas.Dominio.Eventos;

public sealed record ReservaConfirmada(Guid ReservaId, DateTime OcurridoEnUtc) : IEventoDominio;
