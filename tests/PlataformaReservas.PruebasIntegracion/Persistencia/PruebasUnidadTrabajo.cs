using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Abstracciones;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.Infraestructura.Persistencia;
using PlataformaReservas.PruebasIntegracion.Infraestructura;

namespace PlataformaReservas.PruebasIntegracion.Persistencia;

[Collection("BaseDatos")]
public sealed class PruebasUnidadTrabajo(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Una_transaccion_sin_confirmar_no_deja_nada()
    {
        Categoria categoria = DominioPrueba.Categoria(DominioPrueba.Sufijo());

        await using (AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope())
        {
            IUnidadTrabajo unidad = ambito.ServiceProvider.GetRequiredService<IUnidadTrabajo>();
            await using ITransaccion transaccion = await unidad.IniciarTransaccionAsync(CancellationToken.None);
            ambito.ServiceProvider.GetRequiredService<ContextoDatos>().Add(categoria);
            await unidad.GuardarCambiosAsync(CancellationToken.None);
        }

        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Categorias.AnyAsync(c => c.Id == categoria.Id))).Should().BeFalse();
    }

    [Fact]
    public async Task Una_transaccion_confirmada_guarda_los_cambios()
    {
        Categoria categoria = DominioPrueba.Categoria(DominioPrueba.Sufijo());

        await using (AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope())
        {
            IUnidadTrabajo unidad = ambito.ServiceProvider.GetRequiredService<IUnidadTrabajo>();
            await using ITransaccion transaccion = await unidad.IniciarTransaccionAsync(CancellationToken.None);
            ambito.ServiceProvider.GetRequiredService<ContextoDatos>().Add(categoria);
            await unidad.GuardarCambiosAsync(CancellationToken.None);
            await transaccion.ConfirmarAsync(CancellationToken.None);
        }

        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Categorias.AnyAsync(c => c.Id == categoria.Id))).Should().BeTrue();
    }

    [Fact]
    public async Task Dos_reservas_solapadas_del_mismo_profesional_lanzan_hueco_ocupado()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva primera = escenario.Reserva(EscenarioReserva.NueveLunesUtc);
        Reserva solapada = escenario.Reserva(EscenarioReserva.NueveLunesUtc.AddMinutes(30));

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoDatos>().AddRange(primera, solapada);
        Func<Task> guardar = () => ambito.ServiceProvider.GetRequiredService<IUnidadTrabajo>().GuardarCambiosAsync(CancellationToken.None);

        await guardar.Should().ThrowAsync<HuecoOcupadoException>();
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Reservas.CountAsync(r => r.ProfesionalId == escenario.Profesional.Id)))
            .Should().Be(0);
    }

    [Fact]
    public async Task Tras_un_hueco_ocupado_el_mismo_ambito_puede_guardar_otra_reserva()
    {
        EscenarioReserva escenario = await EscenarioReserva.CrearAsync(baseDatos);
        Reserva primera = escenario.Reserva(EscenarioReserva.NueveLunesUtc);
        Reserva solapada = escenario.Reserva(EscenarioReserva.NueveLunesUtc.AddMinutes(30));
        Reserva libre = escenario.Reserva(EscenarioReserva.NueveLunesUtc.AddHours(2));

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ContextoDatos db = ambito.ServiceProvider.GetRequiredService<ContextoDatos>();
        IUnidadTrabajo unidad = ambito.ServiceProvider.GetRequiredService<IUnidadTrabajo>();
        db.AddRange(primera, solapada);
        Func<Task> guardarSolapadas = () => unidad.GuardarCambiosAsync(CancellationToken.None);
        await guardarSolapadas.Should().ThrowAsync<HuecoOcupadoException>();

        db.Add(libre);
        Func<Task> guardarLibre = () => unidad.GuardarCambiosAsync(CancellationToken.None);

        await guardarLibre.Should().NotThrowAsync();
        (await EscenarioEmpresa.LeerAsync(baseDatos, d => d.Reservas.Where(r => r.ProfesionalId == escenario.Profesional.Id).Select(r => r.Id).ToListAsync()))
            .Should().Equal(libre.Id);
    }

    [Fact]
    public async Task Otro_error_de_la_base_de_datos_sube_sin_traducir()
    {
        string sufijo = DominioPrueba.Sufijo();
        await EscenarioEmpresa.GuardarAsync(baseDatos, DominioPrueba.Categoria(sufijo));

        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoDatos>().Add(DominioPrueba.Categoria(sufijo));
        Func<Task> guardar = () => ambito.ServiceProvider.GetRequiredService<IUnidadTrabajo>().GuardarCambiosAsync(CancellationToken.None);

        await guardar.Should().ThrowAsync<DbUpdateException>();
    }
}
