using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Dominio.Entidades;

public sealed class Empresa : EntidadBase
{
    private const int IntervaloHuecosPorDefecto = 15;
    private const int AntelacionMinimaPorDefecto = 60;
    private const int AntelacionMaximaPorDefecto = 60;

    private const string ZonaHorariaPorDefecto = "Europe/Madrid";

    private Empresa()
    {
    }

    public NombreEmpresa Nombre { get; private set; } = null!;

    public Slug Slug { get; private set; } = null!;

    public DescripcionEmpresa? Descripcion { get; private set; }

    public Guid CategoriaId { get; private set; }

    public Email Email { get; private set; } = null!;

    public Telefono Telefono { get; private set; } = null!;

    public Direccion Direccion { get; private set; } = null!;

    public CodigoPostal CodigoPostal { get; private set; } = null!;

    public Ciudad Ciudad { get; private set; } = null!;

    public string ZonaHoraria { get; private set; } = null!;

    public PoliticasReserva Politicas { get; private set; } = null!;

    public bool Activa { get; private set; }

    public DateTime FechaAlta { get; private set; }

    public DateTime FechaModificacion { get; private set; }

    public static Empresa Crear(
        Guid id,
        NombreEmpresa nombre,
        Slug slug,
        Guid categoriaId,
        DescripcionEmpresa? descripcion,
        Email email,
        Telefono telefono,
        Direccion direccion,
        CodigoPostal codigoPostal,
        Ciudad ciudad,
        DateTime ahoraUtc)
    {
        Validacion.IdentificadorObligatorio(id, "La empresa");
        Validacion.IdentificadorObligatorio(categoriaId, "La categoria de la empresa");
        Validacion.InstanteUtc(ahoraUtc, "La fecha de alta de la empresa");

        return new Empresa
        {
            Id = id,
            Nombre = nombre,
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
