using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Dominio.Entidades;

// Inquilino del SaaS. En la interfaz publica se llama "establecimiento" (SPEC §3.2).
// Sus datos NO se cifran: se publican en la ficha y Ciudad debe poder buscarse (RN-164).
public sealed class Empresa : EntidadBase
{
    // Valores por defecto de las politicas en el alta (RN-14).
    private const int IntervaloHuecosPorDefecto = 15;
    private const int AntelacionMinimaPorDefecto = 60;
    private const int AntelacionMaximaPorDefecto = 60;

    // Fijo en el MVP. Punto de extension documentado: no se lee (RN-103).
    private const string ZonaHorariaPorDefecto = "Europe/Madrid";

    private Empresa()
    {
    }

    public string Nombre { get; private set; } = null!;

    public Slug Slug { get; private set; } = null!;

    public string? Descripcion { get; private set; }

    public Guid CategoriaId { get; private set; }

    public string Email { get; private set; } = null!;

    public string Telefono { get; private set; } = null!;

    public string Direccion { get; private set; } = null!;

    public string CodigoPostal { get; private set; } = null!;

    public string Ciudad { get; private set; } = null!;

    public string ZonaHoraria { get; private set; } = null!;

    public PoliticasReserva Politicas { get; private set; } = null!;

    public bool Activa { get; private set; }

    public DateTime FechaAlta { get; private set; }

    public DateTime FechaModificacion { get; private set; }

    public static Empresa Crear(
        Guid id,
        string nombre,
        Slug slug,
        Guid categoriaId,
        string? descripcion,
        string email,
        string telefono,
        string direccion,
        string codigoPostal,
        string ciudad,
        DateTime ahoraUtc)
    {
        Validacion.IdentificadorObligatorio(id, "La empresa");
        Validacion.TextoObligatorio(nombre, 120, "El nombre de la empresa");
        Validacion.IdentificadorObligatorio(categoriaId, "La categoria de la empresa");
        Validacion.InstanteUtc(ahoraUtc, "La fecha de alta de la empresa");

        return new Empresa
        {
            Id = id,
            Nombre = nombre.Trim(),
            Slug = slug,
            Descripcion = descripcion,
            CategoriaId = categoriaId,
            Email = email,
            Telefono = telefono,
            Direccion = direccion,
            CodigoPostal = codigoPostal,
            Ciudad = ciudad,
            ZonaHoraria = ZonaHorariaPorDefecto,
            Politicas = new PoliticasReserva(
                IntervaloHuecosPorDefecto,
                AntelacionMinimaPorDefecto,
                AntelacionMaximaPorDefecto),
            Activa = true,
            FechaAlta = ahoraUtc,
            FechaModificacion = ahoraUtc,
        };
    }
}
