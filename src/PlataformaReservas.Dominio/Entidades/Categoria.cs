using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Dominio.Entidades;

public sealed class Categoria : EntidadBase
{
    private Categoria()
    {
    }

    public string Nombre { get; private set; } = null!;

    public Slug Slug { get; private set; } = null!;

    public bool Activa { get; private set; }

    public static Categoria Crear(Guid id, string nombre, Slug slug)
    {
        Validacion.IdentificadorObligatorio(id, "La categoria");
        Validacion.TextoObligatorio(nombre, 80, "El nombre de la categoria");

        if (slug.Valor.Length > 80)
        {
            throw new DominioException("El slug de la categoria no puede superar 80 caracteres.");
        }

        return new Categoria
        {
            Id = id,
            Nombre = nombre.Trim(),
            Slug = slug,
            Activa = true,
        };
    }
}
