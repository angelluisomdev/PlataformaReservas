using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.ValueObjects;

// Hora local de la empresa, sin conversion (RN-101).
public sealed record IntervaloHorario
{
    public IntervaloHorario(TimeOnly horaInicio, TimeOnly horaFin)
    {
        if (horaInicio >= horaFin)
        {
            throw new DominioException("La hora de inicio debe ser anterior a la hora de fin (RN-41).");
        }

        HoraInicio = horaInicio;
        HoraFin = horaFin;
    }

    public TimeOnly HoraInicio { get; }

    public TimeOnly HoraFin { get; }

    public int DuracionMinutos => (int)(HoraFin - HoraInicio).TotalMinutes;
}
