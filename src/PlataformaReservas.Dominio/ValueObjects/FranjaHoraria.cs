using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.ValueObjects;

public sealed record FranjaHoraria
{
    public FranjaHoraria(DateTime inicioUtc, DateTime finUtc)
    {
        if (inicioUtc.Kind != DateTimeKind.Utc || finUtc.Kind != DateTimeKind.Utc)
        {
            throw new DominioException("Los instantes de una franja horaria deben estar en UTC.");
        }

        if (finUtc <= inicioUtc)
        {
            throw new DominioException("El fin de la franja horaria debe ser posterior a su inicio.");
        }

        InicioUtc = inicioUtc;
        FinUtc = finUtc;
    }

    public DateTime InicioUtc { get; }

    public DateTime FinUtc { get; }

    public int DuracionMinutos => (int)(FinUtc - InicioUtc).TotalMinutes;

    public bool SeSolapaCon(FranjaHoraria otra)
        => InicioUtc < otra.FinUtc && FinUtc > otra.InicioUtc;
}
