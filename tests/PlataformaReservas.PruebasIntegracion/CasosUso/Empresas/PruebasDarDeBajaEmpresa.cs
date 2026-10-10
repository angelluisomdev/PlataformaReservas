using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlataformaReservas.Aplicacion.Compartido;
using PlataformaReservas.Dominio.Entidades;
using PlataformaReservas.PruebasIntegracion.Infraestructura;
using Microsoft.AspNetCore.Identity;
using PlataformaReservas.Aplicacion.CasosUso.Empresas;
using PlataformaReservas.Dominio.Enumeraciones;
using PlataformaReservas.Dominio.ValueObjects;
using PlataformaReservas.Infraestructura.Identidad;
using PlataformaReservas.Infraestructura.Persistencia;

namespace PlataformaReservas.PruebasIntegracion.CasosUso.Empresas;

[Collection("BaseDatos")]
public sealed class PruebasDarDeBajaEmpresa(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Cancela_solo_las_reservas_futuras_confirmadas_y_deja_intactas_las_demas()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Usuario usuario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        Servicio servicio = DominioPrueba.Servicio(empresa.Id);
        Profesional profesional = DominioPrueba.Profesional(empresa.Id);
        Reserva futura1 = Nueva(empresa.Id, usuario.Id, profesional.Id, servicio, DominioPrueba.Ahora.AddDays(1));
        Reserva futura2 = Nueva(empresa.Id, usuario.Id, profesional.Id, servicio, DominioPrueba.Ahora.AddDays(2));
        Reserva pasada = Nueva(empresa.Id, usuario.Id, profesional.Id, servicio, DominioPrueba.Ahora.AddDays(-1));
        Reserva completada = Nueva(empresa.Id, usuario.Id, profesional.Id, servicio, DominioPrueba.Ahora.AddDays(-2));
        await EscenarioEmpresa.GuardarAsync(baseDatos, servicio, profesional, futura1, futura2, pasada, completada);
        await MarcarCompletadaAsync(completada.Id);

        Resultado<int> resultado = await DarDeBajaAsync(empresa.Id);

        resultado.EsExito.Should().BeTrue();
        resultado.Valor.Should().Be(2);
        (await EstadoAsync(futura1.Id)).Should().Be(EstadoReserva.Cancelada);
        (await EstadoAsync(futura2.Id)).Should().Be(EstadoReserva.Cancelada);
        (await EstadoAsync(pasada.Id)).Should().Be(EstadoReserva.Confirmada);
        (await EstadoAsync(completada.Id)).Should().Be(EstadoReserva.Completada);
        (await EscenarioEmpresa.LeerAsync(baseDatos, db => db.Empresas.SingleAsync(e => e.Id == empresa.Id))).Activa.Should().BeFalse();
    }

    [Fact]
    public async Task Renueva_el_sello_del_propietario()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        Usuario propietario = await UsuariosPrueba.CrearAsync(baseDatos, UsuariosPrueba.CorreoUnico());
        await EscenarioEmpresa.GuardarAsync(
            baseDatos, MiembroEmpresa.Crear(Guid.CreateVersion7(), empresa.Id, propietario.Id, RolMiembro.Propietario, DominioPrueba.Ahora));
        string? antes = await SelloAsync(propietario.Id);

        await DarDeBajaAsync(empresa.Id);

        (await SelloAsync(propietario.Id)).Should().NotBe(antes);
    }

    [Fact]
    public async Task Una_empresa_ya_dada_de_baja_da_estado_no_valido()
    {
        Empresa empresa = await EscenarioEmpresa.CrearEmpresaAsync(baseDatos);
        await DarDeBajaAsync(empresa.Id);

        Resultado<int> otraVez = await DarDeBajaAsync(empresa.Id);

        otraVez.Codigo.Should().Be(CodigoError.EstadoNoValido);
    }

    private static Reserva Nueva(Guid empresaId, Guid usuarioId, Guid profesionalId, Servicio servicio, DateTime inicioUtc)
    {
        return Reserva.Crear(
            Guid.CreateVersion7(), empresaId, usuarioId, profesionalId, servicio, inicioUtc,
            new NombrePersona("Lucia Fernandez"), new Telefono("699887766"), null, DominioPrueba.Ahora);
    }

    private async Task<Resultado<int>> DarDeBajaAsync(Guid empresaId)
    {
        await using AsyncServiceScope ambito = baseDatos.AmbitoDeEmpresa(empresaId);
        return await ambito.ServiceProvider.GetRequiredService<DarDeBajaEmpresa>().EjecutarAsync(CancellationToken.None);
    }

    private async Task MarcarCompletadaAsync(Guid reservaId)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        await ambito.ServiceProvider.GetRequiredService<ContextoDatos>().Database
            .ExecuteSqlInterpolatedAsync($"UPDATE reservas SET estado = {(int)EstadoReserva.Completada} WHERE id = {reservaId}");
    }

    private Task<EstadoReserva> EstadoAsync(Guid reservaId)
    {
        return EscenarioEmpresa.LeerAsync(
            baseDatos, db => db.Reservas.Where(r => r.Id == reservaId).Select(r => r.Estado).SingleAsync());
    }

    private async Task<string?> SelloAsync(Guid usuarioId)
    {
        await using AsyncServiceScope ambito = baseDatos.ServiciosIdentidad.CreateAsyncScope();
        UserManager<Usuario> usuarios = ambito.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        return (await usuarios.FindByIdAsync(usuarioId.ToString()))!.SecurityStamp;
    }
}
