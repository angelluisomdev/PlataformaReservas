using Microsoft.EntityFrameworkCore;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.CasosUso.Empresas;
using PlataformaReservas.Aplicacion.Dtos.Categorias;
using PlataformaReservas.Aplicacion.Dtos.Empresas;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.Infraestructura.Consultas;

public sealed class ConsultasCatalogoPublico(ContextoDatos db) : IConsultasCatalogoPublico, IConsultasReservaPublica
{
    public async Task<IReadOnlyList<CategoriaDto>> ObtenerCategoriasAsync(CancellationToken ct)
    {
        return await db.Categorias
            .Where(c => c.Activa)
            .OrderBy(c => c.Nombre)
            .Select(c => CategoriaDto.ConvertirADto(c))
            .ToListAsync(ct);
    }

    public async Task<PaginaResultados<ResumenEmpresaDto>> BuscarAsync(
        string ciudad, string? categoriaSlug, int pagina, int tamanoPagina, CancellationToken ct)
    {
        int tamano = Math.Clamp(tamanoPagina, 1, BuscarEstablecimientos.TamanoPaginaMaximo);

        List<Categoria> categorias = await db.Categorias.Where(c => c.Activa).ToListAsync(ct);
        Categoria? filtro = categoriaSlug is null ? null : categorias.FirstOrDefault(c => c.Slug.Valor == categoriaSlug);
        if (categoriaSlug is not null && filtro is null)
        {
            return new PaginaResultados<ResumenEmpresaDto>([], 0, pagina, tamano);
        }

        Ciudad buscada = new(ciudad);
        IQueryable<Empresa> consulta = Visibles().Where(e => e.Ciudad == buscada);
        if (filtro is not null)
        {
            consulta = consulta.Where(e => e.CategoriaId == filtro.Id);
        }

        int total = await consulta.CountAsync(ct);
        long desplazamiento = (long)(pagina - 1) * tamano;
        if (desplazamiento >= total)
        {
            return new PaginaResultados<ResumenEmpresaDto>([], total, pagina, tamano);
        }

        List<Empresa> empresas = await consulta
            .OrderBy(e => e.Nombre)
            .ThenBy(e => e.Slug)
            .Skip((int)desplazamiento)
            .Take(tamano)
            .ToListAsync(ct);

        Dictionary<Guid, string> nombres = categorias.ToDictionary(c => c.Id, c => c.Nombre.Valor);
        List<ResumenEmpresaDto> elementos = empresas
            .Select(e => ResumenEmpresaDto.ConvertirADto(e, nombres.GetValueOrDefault(e.CategoriaId, "")))
            .ToList();
        return new PaginaResultados<ResumenEmpresaDto>(elementos, total, pagina, tamano);
    }

    public async Task<FichaEmpresaDto?> ObtenerFichaAsync(string slug, CancellationToken ct)
    {
        (Empresa Empresa, List<Servicio> Servicios)? visible = await VisibleConServiciosAsync(slug, ct);
        if (visible is not { Empresa: Empresa empresa, Servicios: List<Servicio> servicios })
        {
            return null;
        }

        NombreCategoria categoria = await db.Categorias
            .Where(c => c.Id == empresa.CategoriaId)
            .Select(c => c.Nombre)
            .FirstAsync(ct);
        List<Profesional> profesionales = await db.Profesionales
            .Include(p => p.Horarios)
            .Where(p => p.EmpresaId == empresa.Id && p.Activo)
            .ToListAsync(ct);
        return FichaEmpresaDto.ConvertirADto(empresa, categoria.Valor, servicios, profesionales);
    }

    public async Task<DatosReservaDto?> ObtenerDatosReservaAsync(string slug, CancellationToken ct)
    {
        (Empresa Empresa, List<Servicio> Servicios)? visible = await VisibleConServiciosAsync(slug, ct);
        if (visible is not { Empresa: Empresa empresa, Servicios: List<Servicio> servicios })
        {
            return null;
        }

        List<Profesional> profesionales = await db.Profesionales
            .Where(p => p.EmpresaId == empresa.Id && p.Activo)
            .ToListAsync(ct);
        return DatosReservaDto.ConvertirADto(empresa, servicios, profesionales);
    }

    private async Task<(Empresa Empresa, List<Servicio> Servicios)?> VisibleConServiciosAsync(string slug, CancellationToken ct)
    {
        Slug? buscado = SlugExacto(slug);
        if (buscado is null)
        {
            return null;
        }

        Empresa? empresa = await Visibles().FirstOrDefaultAsync(e => e.Slug == buscado, ct);
        if (empresa is null)
        {
            return null;
        }

        List<Servicio> servicios = await db.Servicios
            .Where(s => s.EmpresaId == empresa.Id && s.Activo)
            .ToListAsync(ct);
        return (empresa, servicios);
    }

    private IQueryable<Empresa> Visibles()
    {
        return db.Empresas.Where(e => e.Activa).Where(CondicionPublicable.Aplica(db));
    }

    private static Slug? SlugExacto(string texto)
    {
        try
        {
            Slug slug = Slug.Desde(texto);
            return slug.Valor == texto ? slug : null;
        }
        catch (DominioException)
        {
            return null;
        }
    }
}
