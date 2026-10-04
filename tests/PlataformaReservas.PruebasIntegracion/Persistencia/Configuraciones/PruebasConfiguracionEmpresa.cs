using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Persistencia.Configuraciones;

[Collection("BaseDatos")]
public sealed class PruebasConfiguracionEmpresa(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Una_empresa_guardada_se_relee_con_todos_sus_objetos_de_valor()
    {
        string sufijo = DominioPrueba.Sufijo();
        Categoria categoria = DominioPrueba.Categoria(sufijo);
        Empresa empresa = DominioPrueba.Empresa(sufijo, categoria.Id, new DescripcionEmpresa("Cortes clasicos"));
        await GuardarAsync(categoria, empresa);

        Empresa leida = await LeerAsync(empresa.Id);

        leida.Nombre.Should().Be(empresa.Nombre);
        leida.Slug.Should().Be(empresa.Slug);
        leida.Descripcion.Should().Be(empresa.Descripcion);
        leida.Email.Should().Be(empresa.Email);
        leida.Telefono.Should().Be(empresa.Telefono);
        leida.Direccion.Should().Be(empresa.Direccion);
        leida.CodigoPostal.Should().Be(empresa.CodigoPostal);
        leida.Ciudad.Should().Be(empresa.Ciudad);
        leida.Politicas.Should().Be(empresa.Politicas);
        leida.FechaAlta.Should().Be(empresa.FechaAlta);
    }

    [Fact]
    public async Task Una_empresa_sin_descripcion_se_relee_con_descripcion_nula()
    {
        string sufijo = DominioPrueba.Sufijo();
        Categoria categoria = DominioPrueba.Categoria(sufijo);
        Empresa empresa = DominioPrueba.Empresa(sufijo, categoria.Id);
        await GuardarAsync(categoria, empresa);

        Empresa leida = await LeerAsync(empresa.Id);

        leida.Descripcion.Should().BeNull();
    }

    [Fact]
    public async Task Se_puede_filtrar_por_ciudad_con_el_objeto_de_valor()
    {
        string sufijo = DominioPrueba.Sufijo();
        Categoria categoria = DominioPrueba.Categoria(sufijo);
        Empresa empresa = DominioPrueba.Empresa(sufijo, categoria.Id);
        await GuardarAsync(categoria, empresa);
        Ciudad ciudad = new("Salamanca");

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();
        List<Guid> encontradas = await db.Empresas
            .Where(e => e.Ciudad == ciudad && e.CategoriaId == categoria.Id)
            .Select(e => e.Id)
            .ToListAsync();

        encontradas.Should().ContainSingle().Which.Should().Be(empresa.Id);
    }

    private async Task GuardarAsync(params object[] entidades)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();
        db.AddRange(entidades);
        await db.SaveChangesAsync();
    }

    private async Task<Empresa> LeerAsync(Guid id)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();
        return await db.Empresas.SingleAsync(e => e.Id == id);
    }
}
