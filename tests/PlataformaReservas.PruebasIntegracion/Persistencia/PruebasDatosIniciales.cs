using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.Infraestructura.Persistencia.DatosIniciales;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Persistencia;

[Collection("BaseDatos")]
public sealed class PruebasDatosIniciales(BaseDatosFixture baseDatos)
{
    private static readonly string[] SlugsEsperados =
    [
        "peluqueria", "barberia", "estetica", "fisioterapia",
        "psicologia", "taller", "academia", "entrenamiento-personal",
    ];

    [Fact]
    public async Task Sembrar_dos_veces_deja_las_ocho_categorias_una_sola_vez()
    {
        await using (ContextoDatos db = baseDatos.CrearContexto())
        {
            await SembradorDatos.SembrarAsync(db, CancellationToken.None);
        }

        await using (ContextoDatos db = baseDatos.CrearContexto())
        {
            await SembradorDatos.SembrarAsync(db, CancellationToken.None);
        }

        await using ContextoDatos lectura = baseDatos.CrearContexto();
        List<Slug> sembradas = await lectura.Categorias.Select(c => c.Slug).ToListAsync();

        sembradas.Select(s => s.Valor)
            .Where(v => SlugsEsperados.Contains(v))
            .Should().BeEquivalentTo(SlugsEsperados);
    }

    [Fact]
    public async Task Los_datos_de_demostracion_solo_se_siembran_habilitados_desde_el_arranque_sin_clave_no_crean_usuarios_y_son_idempotentes()
    {
        const string clave = "ClaveDemo123!";
        await using (ContextoDatos db = baseDatos.CrearContexto())
        {
            await SembradorDatos.SembrarAsync(db, CancellationToken.None);
        }

        await SembrarDemostracionAsync(new Dictionary<string, string?>());
        (await EmpresasDemostracionAsync()).Should().BeEmpty("deshabilitado por defecto no siembra nada");

        await SembrarDemostracionAsync(new Dictionary<string, string?> { ["DatosDemostracion:Habilitado"] = "true" });
        (await EmpresasDemostracionAsync()).Should().HaveCount(2);
        (await UsuarioAsync(SembradorDatos.CorreoClienteDemostracion)).Should().BeNull("sin clave no se crea ningun usuario");
        (await TodasLasEmpresasDemostracionAsync()).Should().Be(CatalogoDemostracion.Empresas.Count, "se siembra el catalogo entero");
        (await EstadosReservasDemostracionAsync()).Should().BeEmpty("sin clave no hay clientes ni reservas");

        await using (AsyncServiceScope previo = baseDatos.ServiciosIdentidad.CreateAsyncScope())
        {
            Resultado<Guid> sinEmpresa = await previo.ServiceProvider.GetRequiredService<IServicioCuentas>().CrearPropietarioAsync(
                new Email(PropietariosDemostracion[0].Correo), new NombrePersona("Carlos Martin"), null, clave, DominioPrueba.Ahora,
                CancellationToken.None);
            sinEmpresa.EsExito.Should().BeTrue("simula una siembra interrumpida entre el usuario y su pertenencia");
        }

        Dictionary<string, string?> conClave = new()
        {
            ["DatosDemostracion:Habilitado"] = "true",
            ["DatosDemostracion:Clave"] = clave,
        };
        await using (ServiceProvider arranque = baseDatos.CrearServicios(conClave))
        {
            await arranque.MigrarYSembrarAsync(CancellationToken.None);
        }

        (await UsuarioAsync(SembradorDatos.CorreoClienteDemostracion)).Should().NotBeNull("el arranque siembra la demostracion");
        List<EstadoReserva> reservas = await EstadosReservasDemostracionAsync();
        reservas.Should().Contain([EstadoReserva.Confirmada, EstadoReserva.Completada, EstadoReserva.Cancelada],
            "con clave se siembran reservas pasadas y futuras aunque las empresas nacieran sin clave");
        await SembrarDemostracionAsync(conClave);

        (await EmpresasDemostracionAsync()).Should().HaveCount(2);
        (await TodasLasEmpresasDemostracionAsync()).Should().Be(CatalogoDemostracion.Empresas.Count, "un segundo arranque no duplica empresas");
        (await EstadosReservasDemostracionAsync()).Should().HaveCount(reservas.Count, "un segundo arranque no duplica reservas");
        foreach (ClienteDemostracion demostracion in CatalogoDemostracion.Clientes)
        {
            (await UsuarioAsync(demostracion.Correo)).Should().NotBeNull("{0} es un cliente de demostracion", demostracion.Correo);
        }

        Slug[] slugs = [.. SlugsDemostracion.Select(Slug.Desde)];
        int miembros = await EscenarioEmpresa.LeerAsync(baseDatos, db => db.MiembrosEmpresa
            .CountAsync(m => db.Empresas.Any(e => e.Id == m.EmpresaId && slugs.Contains(e.Slug))));
        miembros.Should().Be(2);
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        IConsultasCatalogoPublico catalogo = ambito.ServiceProvider.GetRequiredService<IConsultasCatalogoPublico>();
        foreach (EmpresaDemostracion empresa in CatalogoDemostracion.Empresas)
        {
            (await catalogo.ObtenerFichaAsync(empresa.Slug, CancellationToken.None)).Should().NotBeNull("{0} tiene que ser publicable", empresa.Slug);
        }

        Usuario cliente = (await UsuarioAsync(SembradorDatos.CorreoClienteDemostracion))!;
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        (await usuarios.CheckPasswordAsync(cliente, clave)).Should().BeTrue();
        (await usuarios.IsInRoleAsync(cliente, RolesIdentidad.Cliente)).Should().BeTrue();
        foreach ((string slug, string correo) in PropietariosDemostracion)
        {
            Usuario propietario = (await UsuarioAsync(correo))!;
            Slug esperado = Slug.Desde(slug);
            List<Slug> empresas = await EscenarioEmpresa.LeerAsync(baseDatos, db => db.MiembrosEmpresa
                .Where(m => m.UsuarioId == propietario.Id)
                .Join(db.Empresas, m => m.EmpresaId, e => e.Id, (m, e) => e.Slug)
                .ToListAsync());
            empresas.Should().Equal([esperado], "{0} es miembro solo de su empresa", correo);
            (await usuarios.CheckPasswordAsync(propietario, clave)).Should().BeTrue();
            (await usuarios.IsInRoleAsync(propietario, RolesIdentidad.Propietario)).Should().BeTrue();
        }
    }

    private static readonly string[] SlugsDemostracion = ["demo-barberia-centro", "demo-estetica-norte"];

    private static readonly (string Slug, string Correo)[] PropietariosDemostracion =
    [
        ("demo-barberia-centro", "propietario.barberia@demo.example"),
        ("demo-estetica-norte", "propietaria.estetica@demo.example"),
    ];

    private async Task SembrarDemostracionAsync(Dictionary<string, string?> valores)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        IConfiguration configuracion = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        await SembradorDatos.SembrarDemostracionAsync(ambito.ServiceProvider, configuracion, CancellationToken.None);
    }

    private Task<List<Slug>> EmpresasDemostracionAsync()
    {
        Slug[] slugs = [.. SlugsDemostracion.Select(Slug.Desde)];
        return EscenarioEmpresa.LeerAsync(baseDatos, db => db.Empresas.Where(e => slugs.Contains(e.Slug)).Select(e => e.Slug).ToListAsync());
    }

    private Task<int> TodasLasEmpresasDemostracionAsync()
    {
        Slug[] slugs = [.. CatalogoDemostracion.Empresas.Select(e => Slug.Desde(e.Slug))];
        return EscenarioEmpresa.LeerAsync(baseDatos, db => db.Empresas.CountAsync(e => slugs.Contains(e.Slug)));
    }

    private Task<List<EstadoReserva>> EstadosReservasDemostracionAsync()
    {
        Slug[] slugs = [.. CatalogoDemostracion.Empresas.Select(e => Slug.Desde(e.Slug))];
        return EscenarioEmpresa.LeerAsync(baseDatos, db => db.Reservas
            .Where(r => db.Empresas.Any(e => e.Id == r.EmpresaId && slugs.Contains(e.Slug)))
            .Select(r => r.Estado)
            .ToListAsync());
    }

    private async Task<Usuario?> UsuarioAsync(string correo)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        return await ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>().FindByEmailAsync(correo);
    }
}
