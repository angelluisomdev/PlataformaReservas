using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.PruebasIntegracion.Infraestructura;

public static class EscenarioEmpresa
{
    public static async Task<Empresa> CrearEmpresaAsync(BaseDatosFixture baseDatos)
    {
        string sufijo = DominioPrueba.Sufijo();
        Categoria categoria = DominioPrueba.Categoria(sufijo);
        Empresa empresa = DominioPrueba.Empresa(sufijo, categoria.Id);

        await GuardarAsync(baseDatos, categoria, empresa);
        return empresa;
    }

    public static async Task<Categoria> CrearCategoriaAsync(BaseDatosFixture baseDatos)
    {
        Categoria categoria = DominioPrueba.Categoria(DominioPrueba.Sufijo());
        await GuardarAsync(baseDatos, categoria);
        return categoria;
    }

    public static async Task GuardarAsync(BaseDatosFixture baseDatos, params object[] entidades)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();
        db.AddRange(entidades);
        await db.SaveChangesAsync();
    }

    public static async Task<T> LeerAsync<T>(BaseDatosFixture baseDatos, Func<ContextoDatos, Task<T>> consulta)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        return await consulta(ambito.ServiceProvider.GetRequiredService<ContextoDatos>());
    }
}
