using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Aplicacion.Dtos.Profesionales;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;

namespace PlataformaReservas.Aplicacion.CasosUso.Profesionales;

public sealed class ListarProfesionales(IRepositorioProfesionales profesionales)
{
    public async Task<Resultado<IReadOnlyList<ProfesionalDto>>> EjecutarAsync(CancellationToken ct)
    {
        IReadOnlyList<Profesional> encontrados = await profesionales.ListarAsync(ct);

        List<ProfesionalDto> dtos = encontrados
            .OrderBy(p => p.NombreCompleto.Valor, StringComparer.CurrentCulture)
            .Select(ProfesionalDto.ConvertirADto)
            .ToList();

        return Resultado<IReadOnlyList<ProfesionalDto>>.Exito(dtos);
    }
}
