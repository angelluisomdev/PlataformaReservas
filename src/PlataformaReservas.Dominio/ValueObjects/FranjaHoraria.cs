using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.ValueObjects;

// Instantes absolutos en UTC. Aqui vive la regla de solapamiento, en un unico punto (RN-51).
public sealed record FranjaHoraria
{
    public FranjaHoraria(DateTime inicioUtc, DateTime finUtc)
    {
        // Npgsql rechaza en tiempo de ejecucion cualquier DateTime no UTC destinado a timestamptz (RN-100).
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

    // Intervalo semiabierto [inicio, fin): misma semantica que tstzrange(..., '[)') && en PostgreSQL (SPEC §12.4).
    public bool SeSolapaCon(FranjaHoraria otra)
        => InicioUtc < otra.FinUtc && FinUtc > otra.InicioUtc;
}
