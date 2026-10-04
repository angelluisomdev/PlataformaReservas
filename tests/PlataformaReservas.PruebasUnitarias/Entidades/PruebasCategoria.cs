using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;

namespace PlataformaReservas.PruebasUnitarias.Entidades;

public sealed class PruebasCategoria
{
    private static readonly NombreCategoria Nombre = new("Peluquerias");
    private static readonly Slug SlugValido = Slug.Desde("peluquerias");

    [Fact]
    public void Nace_activa_con_su_nombre_y_su_slug()
    {
        Categoria categoria = Categoria.Crear(Guid.CreateVersion7(), Nombre, SlugValido);

        categoria.Activa.Should().BeTrue();
        categoria.Nombre.Should().Be(Nombre);
        categoria.Slug.Should().Be(SlugValido);
    }

    [Fact]
    public void Admite_un_slug_de_80_caracteres()
    {
        Action crear = () => Categoria.Crear(Guid.CreateVersion7(), Nombre, Slug.Desde(new string('a', 80)));

        crear.Should().NotThrow();
    }

    [Fact]
    public void Rechaza_un_slug_de_mas_de_80_caracteres()
    {
        Slug largo = Slug.Desde(new string('a', 81));

        Action crear = () => Categoria.Crear(Guid.CreateVersion7(), Nombre, largo);

        crear.Should().Throw<DominioException>();
    }

    [Fact]
    public void Rechaza_un_identificador_vacio()
    {
        Action crear = () => Categoria.Crear(Guid.Empty, Nombre, SlugValido);

        crear.Should().Throw<DominioException>();
    }
}
