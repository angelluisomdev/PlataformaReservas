namespace PlataformaReservas.Dominio.Eventos;

public sealed record EmpresaDadaDeBaja(Guid EmpresaId, DateTime OcurridoEnUtc) : IEventoDominio;
