namespace PlataformaReservas.Dominio.Eventos;

public sealed record CuentaRegistrada(Guid UsuarioId, DateTime OcurridoEnUtc) : IEventoDominio;
