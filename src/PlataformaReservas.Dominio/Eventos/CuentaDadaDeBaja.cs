namespace PlataformaReservas.Dominio.Eventos;

public sealed record CuentaDadaDeBaja(Guid UsuarioId, DateTime OcurridoEnUtc) : IEventoDominio;
