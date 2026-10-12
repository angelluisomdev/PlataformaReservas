namespace PlataformaReservas.Aplicacion.Disponibilidad;

public interface IMotorDisponibilidad
{
    Task<IReadOnlyList<HuecoDisponible>> ObtenerHuecosAsync(
        Guid empresaId,
        Guid servicioId,
        Guid? profesionalId,
        DateOnly fecha,
        CancellationToken ct);
}
