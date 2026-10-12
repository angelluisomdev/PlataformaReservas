using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Aplicacion.Dtos.Servicios;

public sealed record ServicioDisponibilidadDto(
    bool Activo, int DuracionMinutos, int? IntervaloHuecosMinutos, int? AntelacionMinimaMinutos, int? AntelacionMaximaDias)
{
    public static ServicioDisponibilidadDto ConvertirADto(Servicio servicio)
    {
        return new ServicioDisponibilidadDto(
            servicio.Activo,
            servicio.Duracion.Minutos,
            servicio.IntervaloHuecosMinutos,
            servicio.AntelacionMinimaMinutos,
            servicio.AntelacionMaximaDias);
    }
}
