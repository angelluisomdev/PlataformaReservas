namespace PlataformaReservas.Dominio.Eventos;

public sealed record EmpresaRegistrada(Guid EmpresaId, DateTime OcurridoEnUtc) : IEventoDominio;
