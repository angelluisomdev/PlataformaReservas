using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos.Servicios;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Aplicacion.Dtos.Empresas;

public sealed record DatosReservaDto(Guid EmpresaId, string Nombre, string Slug, IReadOnlyList<ServicioReservaDto> Servicios)
{
    public static DatosReservaDto ConvertirADto(Empresa empresa, IEnumerable<Servicio> servicios, IEnumerable<Profesional> profesionales)
    {
        List<Profesional> lista = profesionales.ToList();
        return new DatosReservaDto(
            empresa.Id,
            empresa.Nombre.Valor,
            empresa.Slug.Valor,
            servicios
                .OrderBy(s => s.Nombre.Valor, OrdenEspanol.Comparador)
                .Select(s => ServicioReservaDto.ConvertirADto(s, PoliticasReserva.Resolver(empresa, s), lista))
                .ToList());
    }
}
