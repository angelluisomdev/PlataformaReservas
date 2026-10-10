using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Dominio.Entidades;

public sealed class Servicio : EntidadBase
{
    private Servicio()
    {
    }

    public Guid EmpresaId { get; private set; }

    public NombreServicio Nombre { get; private set; } = null!;

    public DescripcionServicio? Descripcion { get; private set; }

    public Duracion Duracion { get; private set; } = null!;

    public Precio Precio { get; private set; } = null!;

    public int? IntervaloHuecosMinutos { get; private set; }

    public int? AntelacionMinimaMinutos { get; private set; }

    public int? AntelacionMaximaDias { get; private set; }

    public bool Activo { get; private set; }

    public DateTime FechaAlta { get; private set; }

    public DateTime FechaModificacion { get; private set; }

    public static Servicio Crear(
        Guid id,
        Guid empresaId,
        NombreServicio nombre,
        Duracion duracion,
        Precio precio,
        DateTime ahoraUtc)
    {
        Validacion.IdentificadorObligatorio(id, "El servicio");
        Validacion.IdentificadorObligatorio(empresaId, "La empresa del servicio");
        Validacion.InstanteUtc(ahoraUtc, "La fecha de alta del servicio");

        return new Servicio
        {
            Id = id,
            EmpresaId = empresaId,
            Nombre = nombre,
            Duracion = duracion,
            Precio = precio,
            Activo = true,
            FechaAlta = ahoraUtc,
            FechaModificacion = ahoraUtc,
        };
    }

    public void ActualizarDatos(
        NombreServicio nombre,
        DescripcionServicio? descripcion,
        Duracion duracion,
        Precio precio,
        DateTime ahoraUtc)
    {
        Validacion.InstanteUtc(ahoraUtc, "La fecha de modificacion del servicio");

        Nombre = nombre;
        Descripcion = descripcion;
        Duracion = duracion;
        Precio = precio;
        FechaModificacion = ahoraUtc;
    }

    public void EstablecerPoliticas(
        IntervaloHuecos? intervaloHuecos,
        AntelacionMinimaReserva? antelacionMinima,
        AntelacionMaximaReserva? antelacionMaxima,
        DateTime ahoraUtc)
    {
        Validacion.InstanteUtc(ahoraUtc, "La fecha de modificacion del servicio");

        IntervaloHuecosMinutos = intervaloHuecos?.Minutos;
        AntelacionMinimaMinutos = antelacionMinima?.Minutos;
        AntelacionMaximaDias = antelacionMaxima?.Dias;
        FechaModificacion = ahoraUtc;
    }

    public void Desactivar(DateTime ahoraUtc)
    {
        Validacion.InstanteUtc(ahoraUtc, "La fecha de modificacion del servicio");

        if (!Activo)
        {
            throw new DominioException("El servicio ya esta desactivado.");
        }

        Activo = false;
        FechaModificacion = ahoraUtc;
    }
}
