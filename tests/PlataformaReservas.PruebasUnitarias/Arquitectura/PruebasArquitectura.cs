using System.Reflection;
using FluentAssertions;
using PlataformaReservas.Dominio.Compartido;

namespace PlataformaReservas.PruebasUnitarias.Arquitectura;

public sealed class PruebasArquitectura
{
    // El ancla es EntidadBase, no una entidad concreta: existe desde la Fase 4,
    // antes que cualquier entidad, y es la unica clase que no va a desaparecer.
    static readonly Assembly EnsambladoDominio = typeof(EntidadBase).Assembly;

    static IEnumerable<Type> Entidades() =>
        EnsambladoDominio.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(EntidadBase).IsAssignableFrom(t));

    [Fact]
    public void Ninguna_entidad_expone_setters_publicos()
    {
        Entidades()
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Where(p => p.SetMethod is { IsPublic: true })      // detecta tambien init
            .Select(p => $"{p.DeclaringType!.Name}.{p.Name}")
            .Should().BeEmpty();
    }

    [Fact]
    public void Ninguna_entidad_expone_colecciones_mutables()
    {
        var mutables = new[] { typeof(List<>), typeof(ICollection<>), typeof(HashSet<>), typeof(ISet<>) };

        Entidades()
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Where(p => p.PropertyType.IsGenericType
                     && mutables.Contains(p.PropertyType.GetGenericTypeDefinition()))
            .Select(p => $"{p.DeclaringType!.Name}.{p.Name}")
            .Should().BeEmpty();
    }

    [Fact]
    public void Toda_entidad_tiene_constructor_privado_sin_parametros()
    {
        // Mismo patron que las otras tres: se buscan las infractoras y se exige que no haya.
        // No se usa OnlyContain: FluentAssertions falla con una coleccion vacia y la
        // suite quedaria en rojo mientras no existan entidades.
        Entidades()
            .Where(t => t.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance,
                                         null, Type.EmptyTypes, null) is null)
            .Select(t => t.Name)
            .Should().BeEmpty();
    }

    [Fact]
    public void El_dominio_no_depende_de_infraestructura()
    {
        EnsambladoDominio.GetReferencedAssemblies().Select(a => a.Name!)
            .Should().NotContain(n => n.StartsWith("Microsoft.EntityFrameworkCore")
                                   || n.StartsWith("Npgsql")
                                   || n.StartsWith("Microsoft.AspNetCore"));
    }
}
