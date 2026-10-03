using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Infraestructura.Persistencia.DatosIniciales;

public static class SembradorDatos
{
    private static readonly string[] _categorias =
    [
        "Peluquería",
        "Barbería",
        "Estética",
        "Fisioterapia",
        "Psicología",
        "Taller",
        "Academia",
        "Entrenamiento personal",
    ];

    public static async Task MigrarYSembrarAsync(this IServiceProvider servicios, CancellationToken ct)
    {
        using IServiceScope ambito = servicios.CreateScope();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();

        await db.Database.MigrateAsync(ct);
        await SembrarAsync(db, ct);
    }

    public static async Task SembrarAsync(ContextoDatos db, CancellationToken ct)
    {
        List<Slug> existentes = await db.Categorias
            .Select(c => c.Slug)
            .ToListAsync(ct);

        foreach (string nombre in _categorias)
        {
            Slug slug = Slug.Desde(nombre);
            if (!existentes.Contains(slug))
            {
                db.Categorias.Add(Categoria.Crear(Guid.CreateVersion7(), nombre, slug));
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
