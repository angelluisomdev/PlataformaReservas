using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Dominio.Repositorios;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.Infraestructura.Repositorios;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Repositorios;

[Collection("BaseDatos")]
public sealed class PruebasRepositorioServicios(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Listar_devuelve_solo_los_de_la_empresa_actual()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Servicio delaPropia = DominioPrueba.Servicio(propia.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, delaPropia, DominioPrueba.Servicio(ajena.Id));

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(propia.Id);
        IReadOnlyList<Servicio> listados = await ambito.ServiceProvider.GetRequiredService<IRepositorioServicios>()
            .ListarAsync(CancellationToken.None);

        listados.Should().ContainSingle().Which.Id.Should().Be(delaPropia.Id);
    }

    [Fact]
    public async Task Agregar_uno_de_otra_empresa_lanza()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(propia.Id);
        IRepositorioServicios repositorio = ambito.ServiceProvider.GetRequiredService<IRepositorioServicios>();

        Action agregar = () => repositorio.Agregar(DominioPrueba.Servicio(ajena.Id));

        agregar.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Agregar_uno_de_otra_empresa_registra_un_aviso_con_las_dos_empresas()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        RegistroCaptura<RepositorioServicios> registro = new();

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        RepositorioServicios repositorio = new(
            ambito.ServiceProvider.GetRequiredService<ContextoDatos>(), new ContextoEmpresaFijo(propia.Id), registro);

        Action agregar = () => repositorio.Agregar(DominioPrueba.Servicio(ajena.Id));

        agregar.Should().Throw<InvalidOperationException>();
        (LogLevel nivel, string linea) = registro.Entradas.Should().ContainSingle().Subject;
        nivel.Should().Be(LogLevel.Warning);
        linea.Should().Contain(propia.Id.ToString()).And.Contain(ajena.Id.ToString());
    }

    [Fact]
    public async Task Agregar_uno_propio_lo_guarda()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Servicio nuevo = DominioPrueba.Servicio(propia.Id);

        await using (AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(propia.Id))
        {
            ambito.ServiceProvider.GetRequiredService<IRepositorioServicios>().Agregar(nuevo);
            await ambito.ServiceProvider.GetRequiredService<ContextoDatos>().SaveChangesAsync();
        }

        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Servicios.AnyAsync(x => x.Id == nuevo.Id))).Should().BeTrue();
    }
}
