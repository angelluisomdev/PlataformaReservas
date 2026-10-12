using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Aplicacion.Dtos.Profesionales;

public sealed record ExcepcionHorarioDto(DateOnly Fecha, string? Motivo)
{
    public static ExcepcionHorarioDto ConvertirADto(ExcepcionHorario excepcion)
    {
        return new ExcepcionHorarioDto(excepcion.Fecha, excepcion.Motivo?.Valor);
    }
}
