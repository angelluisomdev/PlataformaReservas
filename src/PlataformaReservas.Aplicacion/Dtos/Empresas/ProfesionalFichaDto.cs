using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Aplicacion.Dtos.Empresas;

public sealed record ProfesionalFichaDto(string Nombre)
{
    public static ProfesionalFichaDto ConvertirADto(Profesional profesional)
    {
        return new ProfesionalFichaDto(profesional.NombreCompleto.Valor);
    }
}
