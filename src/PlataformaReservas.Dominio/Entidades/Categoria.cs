using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.Dominio.Entidades;

// Datos maestros: se cargan por datos iniciales y no se mantienen desde la aplicacion (RN-122).
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

        return new Categoria
        {
            Id = id,
            Nombre = nombre.Trim(),
            Slug = slug,
            Activa = true,
        };
    }
}
