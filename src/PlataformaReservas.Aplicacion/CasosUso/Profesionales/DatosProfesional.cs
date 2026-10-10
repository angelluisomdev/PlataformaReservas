using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.CasosUso.Profesionales;

internal sealed record DatosProfesional(NombrePersona NombreCompleto, Email? Email, Telefono? Telefono)
{
    public static DatosProfesional Desde(string nombreCompleto, string? email, string? telefono)
    {
        return new DatosProfesional(
            new NombrePersona(nombreCompleto),
            string.IsNullOrWhiteSpace(email) ? null : new Email(email),
            string.IsNullOrWhiteSpace(telefono) ? null : new Telefono(telefono));
    }
}
