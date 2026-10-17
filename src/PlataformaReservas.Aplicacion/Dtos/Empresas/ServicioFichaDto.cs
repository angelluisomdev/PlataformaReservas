using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Aplicacion.Dtos.Empresas;

public sealed record ServicioFichaDto(string Nombre, string? Descripcion, int DuracionMinutos, decimal Precio)
{
    public static ServicioFichaDto ConvertirADto(Servicio servicio)
    {
        return new ServicioFichaDto(
            servicio.Nombre.Valor, servicio.Descripcion?.Valor, servicio.Duracion.Minutos, servicio.Precio.Importe);
    }
}
