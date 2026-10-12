using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.Dtos.Reservas;

public sealed record ReservaOcupadaDto(Guid ProfesionalId, DateTime InicioUtc, DateTime FinUtc)
{
    public static ReservaOcupadaDto ConvertirADto(Reserva reserva)
    {
        return ConvertirADto(reserva.ProfesionalId, reserva.Franja);
    }

    public static ReservaOcupadaDto ConvertirADto(Guid profesionalId, FranjaHoraria franja)
    {
        return new ReservaOcupadaDto(profesionalId, franja.InicioUtc, franja.FinUtc);
    }
}
