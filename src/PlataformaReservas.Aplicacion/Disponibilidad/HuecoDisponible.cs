namespace PlataformaReservas.Aplicacion.Disponibilidad;

public sealed record HuecoDisponible(
    Guid ProfesionalId,
    string NombreProfesional,
    DateTime InicioUtc,
    DateTime FinUtc);
