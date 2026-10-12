using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Aplicacion.Dtos.Servicios;

public sealed record ServicioDto(
    Guid Id,
    string Nombre,
    string? Descripcion,
    int DuracionMinutos,
    decimal Precio,
    int? IntervaloHuecosMinutos,
    int? AntelacionMinimaMinutos,
    int? AntelacionMaximaDias,
    bool Activo)
{
    public static ServicioDto ConvertirADto(Servicio servicio)
    {
        return new ServicioDto(
            servicio.Id,
            servicio.Nombre.Valor,
            servicio.Descripcion?.Valor,
            servicio.Duracion.Minutos,
            servicio.Precio.Importe,
            servicio.IntervaloHuecosMinutos,
            servicio.AntelacionMinimaMinutos,
            servicio.AntelacionMaximaDias,
            servicio.Activo);
    }
}
