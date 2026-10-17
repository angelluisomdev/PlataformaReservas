using System.Globalization;
using PlataformaReservas.Dominio.Entidades;

namespace PlataformaReservas.Aplicacion.Dtos.Empresas;

public sealed record FichaEmpresaDto(
    string Nombre,
    string Slug,
    string Categoria,
    string? Descripcion,
    string Direccion,
    string CodigoPostal,
    string Ciudad,
    string Telefono,
    IReadOnlyList<ServicioFichaDto> Servicios,
    IReadOnlyList<ProfesionalFichaDto> Profesionales,
    IReadOnlyList<HorarioDiaDto> Horario)
{
    private static readonly StringComparer OrdenEspanol = StringComparer.Create(CultureInfo.GetCultureInfo("es-ES"), ignoreCase: false);

    public static FichaEmpresaDto ConvertirADto(
        Empresa empresa, string categoria, IEnumerable<Servicio> servicios, IEnumerable<Profesional> profesionales)
    {
        List<Profesional> lista = profesionales.ToList();
        return new FichaEmpresaDto(
            empresa.Nombre.Valor,
            empresa.Slug.Valor,
            categoria,
            empresa.Descripcion?.Valor,
            empresa.Direccion.Valor,
            empresa.CodigoPostal.Valor,
            empresa.Ciudad.Valor,
            empresa.Telefono.Valor,
            servicios.OrderBy(s => s.Nombre.Valor, OrdenEspanol).Select(ServicioFichaDto.ConvertirADto).ToList(),
            lista.OrderBy(p => p.NombreCompleto.Valor, OrdenEspanol).Select(ProfesionalFichaDto.ConvertirADto).ToList(),
            HorarioDiaDto.ConvertirADto(lista.SelectMany(p => p.Horarios)));
    }
}
