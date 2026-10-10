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
public sealed class PruebasRepositorioProfesionales(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Listar_devuelve_solo_los_de_la_empresa_actual()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional delaPropia = DominioPrueba.Profesional(propia.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, delaPropia, DominioPrueba.Profesional(ajena.Id));

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(propia.Id);
        IReadOnlyList<Profesional> listados = await ambito.ServiceProvider.GetRequiredService<IRepositorioProfesionales>()
            .ListarAsync(CancellationToken.None);

        listados.Should().ContainSingle().Which.Id.Should().Be(delaPropia.Id);
    }

    [Fact]
    public async Task Agregar_uno_de_otra_empresa_lanza()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(propia.Id);
        IRepositorioProfesionales repositorio = ambito.ServiceProvider.GetRequiredService<IRepositorioProfesionales>();

        Action agregar = () => repositorio.Agregar(DominioPrueba.Profesional(ajena.Id));

        agregar.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Agregar_uno_propio_lo_guarda()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional nuevo = DominioPrueba.Profesional(propia.Id);

        await using (AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(propia.Id))
        {
            ambito.ServiceProvider.GetRequiredService<IRepositorioProfesionales>().Agregar(nuevo);
            await ambito.ServiceProvider.GetRequiredService<ContextoDatos>().SaveChangesAsync();
        }

        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Profesionales.AnyAsync(x => x.Id == nuevo.Id))).Should().BeTrue();
    }

    [Fact]
    public async Task ObtenerConHorario_trae_los_hijos_y_no_encuentra_uno_de_otra_empresa()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Empresa ajena = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Profesional propio = DominioPrueba.Profesional(propia.Id);
        propio.AgregarIntervalo(Guid.CreateVersion7(), DayOfWeek.Monday, new IntervaloHorario(new TimeOnly(9, 0), new TimeOnly(14, 0)));
        propio.MarcarNoDisponible(Guid.CreateVersion7(), new DateOnly(2026, 12, 25), new MotivoExcepcion("Festivo"));
        Profesional deLaAjena = DominioPrueba.Profesional(ajena.Id);
        await EscenarioEmpresa.GuardarAsync(baseDatos, propio, deLaAjena);

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(propia.Id);
        IRepositorioProfesionales repositorio = ambito.ServiceProvider.GetRequiredService<IRepositorioProfesionales>();
        Profesional? leido = await repositorio.ObtenerConHorarioAsync(propio.Id, CancellationToken.None);
        Profesional? ajeno = await repositorio.ObtenerConHorarioAsync(deLaAjena.Id, CancellationToken.None);

        leido!.Horarios.Should().ContainSingle();
        leido.Excepciones.Should().ContainSingle().Which.Motivo.Should().Be(new MotivoExcepcion("Festivo"));
        ajeno.Should().BeNull();
        ambito.ServiceProvider.GetRequiredService<ContextoDatos>().ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task ObtenerConHorario_de_un_id_inexistente_devuelve_null()
    {
        Empresa propia = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);

        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(propia.Id);
        Profesional? leido = await ambito.ServiceProvider.GetRequiredService<IRepositorioProfesionales>()
            .ObtenerConHorarioAsync(Guid.CreateVersion7(), CancellationToken.None);

        leido.Should().BeNull();
    }
}
