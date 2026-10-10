using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Repositorios;

[Collection("BaseDatos")]
public sealed class PruebasRepositorioEmpresas(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task ExisteSlug_busca_en_toda_la_plataforma()
    {
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(propia.Id);
        IRepositorioEmpresas repositorio = ambito.ServiceProvider.GetRequiredService<IRepositorioEmpresas>();

        (await repositorio.ExisteSlugAsync(ajena.Slug, CancellationToken.None)).Should().BeTrue();
        (await repositorio.ExisteSlugAsync(Slug.Desde($"libre-{DominioPrueba.Sufijo()}"), CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task Agregar_deja_la_empresa_lista_para_guardar()
    {
        Categoria categoria = await EscenarioEmpresa.CrearCategoriaAsync(baseDatos);
        Empresa empresa = DominioPrueba.Empresa(DominioPrueba.Sufijo(), categoria.Id);

        await using (AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope())
        {
            ambito.ServiceProvider.GetRequiredService<IRepositorioEmpresas>().Agregar(empresa);
            await ambito.ServiceProvider.GetRequiredService<ContextoDatos>().SaveChangesAsync();
        }

        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Empresas.AnyAsync(e => e.Id == empresa.Id))).Should().BeTrue();
    }
}
