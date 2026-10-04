using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasIntegracion.Infraestructura;

public static class DominioPrueba
{
    public static readonly DateTime Ahora = new(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc);

    public static string Sufijo() => Guid.CreateVersion7().ToString("N");

    public static Categoria Categoria(string sufijo)
    {
        return Dominio.Entidades.Categoria.Crear(
            Guid.CreateVersion7(), new NombreCategoria($"Categoria {sufijo}"), Slug.Desde($"cat-{sufijo}"));
    }

    public static Empresa Empresa(string sufijo, Guid categoriaId, DescripcionEmpresa? descripcion = null)
    {
        return Dominio.Entidades.Empresa.Crear(
            Guid.CreateVersion7(),
            new NombreEmpresa("Barberia Centro"),
            Slug.Desde($"empresa-{sufijo}"),
            categoriaId,
            descripcion,
            new Email("contacto@barberia.es"),
            new Telefono("+34 600 000 000"),
            new Direccion("Calle Mayor 1"),
            new CodigoPostal("37001"),
            new Ciudad("Salamanca"),
            Ahora);
    }

    public static Servicio Servicio(Guid empresaId)
    {
        return Dominio.Entidades.Servicio.Crear(
            Guid.CreateVersion7(), empresaId, new NombreServicio("Corte y barba"), new Duracion(45), new Precio(18.50m), Ahora);
    }

    public static Profesional Profesional(Guid empresaId)
    {
        return Dominio.Entidades.Profesional.Crear(Guid.CreateVersion7(), empresaId, new NombrePersona("Ana Garcia"), Ahora);
    }
}
