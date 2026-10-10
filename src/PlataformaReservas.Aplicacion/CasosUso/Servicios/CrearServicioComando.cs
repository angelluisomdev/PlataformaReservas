namespace PlataformaReservas.Aplicacion.CasosUso.Servicios;

public sealed record CrearServicioComando(
    string Nombre,
    string? Descripcion,
    int DuracionMinutos,
    decimal Precio,
    int? IntervaloHuecosMinutos,
    int? AntelacionMinimaMinutos,
    int? AntelacionMaximaDias);
