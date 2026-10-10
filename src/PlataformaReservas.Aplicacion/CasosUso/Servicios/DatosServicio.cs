using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.CasosUso.Servicios;

internal sealed record DatosServicio(
    NombreServicio Nombre,
    DescripcionServicio? Descripcion,
    Duracion Duracion,
    Precio Precio,
    IntervaloHuecos? IntervaloHuecos,
    AntelacionMinimaReserva? AntelacionMinima,
    AntelacionMaximaReserva? AntelacionMaxima)
{
    public static DatosServicio Desde(
        string nombre,
        string? descripcion,
        int duracionMinutos,
        decimal precio,
        int? intervaloHuecosMinutos,
        int? antelacionMinimaMinutos,
        int? antelacionMaximaDias)
    {
        return new DatosServicio(
            new NombreServicio(nombre),
            string.IsNullOrWhiteSpace(descripcion) ? null : new DescripcionServicio(descripcion),
            new Duracion(duracionMinutos),
            new Precio(precio),
            intervaloHuecosMinutos is null ? null : new IntervaloHuecos(intervaloHuecosMinutos.Value),
            antelacionMinimaMinutos is null ? null : new AntelacionMinimaReserva(antelacionMinimaMinutos.Value),
            antelacionMaximaDias is null ? null : new AntelacionMaximaReserva(antelacionMaximaDias.Value));
    }
}
