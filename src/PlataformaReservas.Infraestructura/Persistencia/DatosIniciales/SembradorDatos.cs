using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Infraestructura.Persistencia.DatosIniciales;

// Datos iniciales, solo en desarrollo (RN-123). En la Fase 5 solo las categorias (RN-122):
// empresas y usuarios de demostracion necesitan Identity con cifrado y horarios (fases posteriores).
public static class SembradorDatos
{
    // SPEC §9.2.
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

    // Extension de IServiceProvider y no de WebApplication: Infraestructura no conoce el hospedaje web.
    public static async Task MigrarYSembrarAsync(this IServiceProvider servicios, CancellationToken ct)
    {
        using IServiceScope ambito = servicios.CreateScope();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();

        await db.Database.MigrateAsync(ct);
        await SembrarAsync(db, ct);
    }

    // Idempotente: solo inserta las categorias que falten, comparando por slug.
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
