namespace PlataformaReservas.Aplicacion.CasosUso.Profesionales;

public sealed record ActualizarProfesionalComando(Guid Id, string NombreCompleto, string? Email, string? Telefono);
