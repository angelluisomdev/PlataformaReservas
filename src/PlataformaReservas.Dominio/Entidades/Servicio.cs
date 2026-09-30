using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.Dominio.Entidades;

public sealed class Servicio : EntidadBase
{
    private Servicio()
    {
    }

    // Se fija al crear y ningun metodo lo cambia (RN-01).
    public Guid EmpresaId { get; private set; }

    public string Nombre { get; private set; } = null!;

    public string? Descripcion { get; private set; }

    public int DuracionMinutos { get; private set; }

    public decimal Precio { get; private set; }

    // null significa "heredar de la empresa", no "sin limite" (RN-22).
    public int? IntervaloHuecosMinutos { get; private set; }

    public int? AntelacionMinimaMinutos { get; private set; }

    public int? AntelacionMaximaDias { get; private set; }

    public bool Activo { get; private set; }

    public DateTime FechaAlta { get; private set; }

    public DateTime FechaModificacion { get; private set; }

    public static Servicio Crear(
        Guid id,
        Guid empresaId,
        string nombre,
        int duracionMinutos,
        decimal precio,
        DateTime ahoraUtc)
    {
        Validacion.IdentificadorObligatorio(id, "El servicio");
        Validacion.IdentificadorObligatorio(empresaId, "La empresa del servicio");
        Validacion.TextoObligatorio(nombre, 120, "El nombre del servicio");
        Validacion.InstanteUtc(ahoraUtc, "La fecha de alta del servicio");

        if (duracionMinutos <= 0)
        {
            throw new DominioException("La duracion del servicio debe ser mayor que cero (RN-20).");
        }

        if (precio < 0)
        {
            throw new DominioException("El precio del servicio no puede ser negativo (RN-21).");
        }

        return new Servicio
        {
            Id = id,
            EmpresaId = empresaId,
            Nombre = nombre.Trim(),
            DuracionMinutos = duracionMinutos,
            Precio = precio,
            Activo = true,
            FechaAlta = ahoraUtc,
            FechaModificacion = ahoraUtc,
        };
    }
}
