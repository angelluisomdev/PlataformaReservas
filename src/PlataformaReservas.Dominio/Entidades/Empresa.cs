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
        Validacion.TextoOpcional(descripcion, 1000, "La descripcion de la empresa");
        Validacion.IdentificadorObligatorio(categoriaId, "La categoria de la empresa");
        Validacion.TextoObligatorio(email, 160, "El email de la empresa");
        Validacion.TextoObligatorio(telefono, 20, "El telefono de la empresa");
        Validacion.TextoObligatorio(direccion, 200, "La direccion de la empresa");
        Validacion.TextoObligatorio(codigoPostal, 10, "El codigo postal de la empresa");
        Validacion.TextoObligatorio(ciudad, 80, "La ciudad de la empresa");
        Validacion.InstanteUtc(ahoraUtc, "La fecha de alta de la empresa");

        return new Empresa
        {
            Id = id,
            Nombre = nombre.Trim(),
            Slug = slug,
            Descripcion = descripcion?.Trim(),
            CategoriaId = categoriaId,
            Email = email.Trim(),
            Telefono = telefono.Trim(),
            Direccion = direccion.Trim(),
            CodigoPostal = codigoPostal.Trim(),
            Ciudad = ciudad.Trim(),
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
